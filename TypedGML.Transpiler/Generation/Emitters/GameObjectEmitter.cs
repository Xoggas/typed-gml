using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

using TypedGML.Transpiler.Checking;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits <c>@Object</c>-decorated classes as GameMaker object event scripts.
/// </summary>
/// <remarks>
///     Per <c>@Object</c> class this emitter produces:
///     <list type="bullet">
///         <item><c>Objects/obj_Name/Create_0.gml</c> — field defaults, <c>__types</c>, instance methods, ctor body and synthesised <c>ToString</c>/<c>GetType</c></item>
///         <item><c>Objects/obj_Name/EventName_0.gml</c> — for each <c>@NativeEvent</c> method or known override event</item>
///         <item><c>Scripts/obj_Name_Init/…gml</c> — when the constructor has extra parameters beyond those forwarded to <c>base()</c></item>
///     </list>
/// </remarks>
public sealed class GameObjectEmitter : ICodeEmitter
{
    /// <summary>GML event method names that map to GameMaker event file prefixes.</summary>
    private static readonly HashSet<string> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Create", "Destroy", "Step", "Draw", "Alarm", "Collision",
        "KeyDown", "KeyUp", "KeyPressed", "MouseDown", "MouseUp",
        "MouseMoved", "BeginStep", "EndStep", "DrawGui", "PreDraw",
        "PostDraw", "RoomStart", "RoomEnd", "GameStart", "GameEnd",
        "CleanUp", "AsyncSystem", "AsyncHttp", "AsyncSaving"
    };

    /// <inheritdoc/>
    public bool CanEmit(TgmlTypeDecl decl) =>
        decl is TgmlClassDecl cls && cls.IsGameObject;

    /// <inheritdoc/>
    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var cls        = (TgmlClassDecl)decl;
        ctx.CurrentType = cls;
        var objName    = ctx.GmlObjectName(cls);
        var gmlName    = cls.QualifiedName?.Replace(".", "_") ?? cls.Name;
        var stmtEmit   = new StatementEmitter(ctx);
        var createWriter = new GmlWriter();
        var eventFiles = new List<GeneratedFile>();

        // ── Create event ──────────────────────────────────────────────────────
        createWriter.WriteLine($"// Create event for {objName}");
        createWriter.WriteLine($"__types = {{ {EmitHelpers.BuildTypesStruct(cls, ctx)} }};");

        foreach (var ancestor in ctx.GetClassAncestorChain(cls))
            EmitCreateContribution(ancestor, ctx, createWriter);

        EmitCreateContribution(cls, ctx, createWriter);

        // Synthesised ToString (default unless the class defines its own).
        var qualifiedTypeName = cls.QualifiedName ?? cls.Name;
        var hasUserToString   = cls.Methods.Any(m => m.Name == "ToString" && m.Body is not null);
        if (!hasUserToString)
            createWriter.WriteLine($"ToString = function() {{ return \"{qualifiedTypeName}\"; }}");
        createWriter.WriteLine($"GetType = function() {{ return __TYPE_{gmlName}; }}");

        // ── Non-Create event scripts ──────────────────────────────────────────
        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract || method.Body is null)
                continue;
            if (!TryGetNativeEventName(cls, method, ctx, out var eventName))
                continue;
            if (string.Equals(eventName, "Create", StringComparison.OrdinalIgnoreCase))
                continue;

            var w = new GmlWriter();
            ctx.CurrentMethodName       = method.Name;
            ctx.CurrentMethodIsOverride = method.IsOverride;
            ctx.CurrentMethodOwnerType  = cls;
            ctx.CurrentNativeEventName  = eventName;
            stmtEmit.EmitBlock(method.Body, w);
            ClearMethodContext(ctx);
            eventFiles.Add(new GeneratedFile($"Objects/{objName}/{eventName}_0.gml", w.ToString()));
        }

        yield return new GeneratedFile($"Objects/{objName}/Create_0.gml", createWriter.ToString());
        foreach (var ef in eventFiles)
            yield return ef;

        // ── Init script (extra ctor params beyond base() args) ────────────────
        var initScriptName = $"{objName}_Init";
        foreach (var ctor in cls.Constructors.Where(c => c.Params.Count > 0))
        {
            if (ctor.Body is null) continue;

            var baseArgNames = new HashSet<string>(
                GetNormalizedBaseArgs(ctor).OfType<TgmlIdExpr>().Select(e => e.Name),
                StringComparer.Ordinal);
            var extraParams = ctor.Params.Where(p => !baseArgNames.Contains(p.Name)).ToList();
            if (extraParams.Count == 0) continue;

            var w        = new GmlWriter();
            var paramStr = string.Join(", ", new[] { "inst" }.Concat(extraParams.Select(p => p.Name)));
            w.WriteLine($"function {initScriptName}({paramStr})");
            w.OpenBrace();

            var prevAlias = ctx.SelfAlias;
            ctx.SelfAlias = "inst";
            stmtEmit.EmitBlock(ctor.Body, w);
            ctx.SelfAlias = prevAlias;

            w.CloseBrace();
            yield return new GeneratedFile($"Scripts/{initScriptName}/{initScriptName}.gml", w.ToString());
        }
    }

    // ── Create event contribution ─────────────────────────────────────────────

    /// <summary>
    ///     Emits field defaults, backing fields, instance methods, property accessors and
    ///     the default (no-arg) constructor body for <paramref name="sourceClass"/> into the
    ///     Create event writer.
    /// </summary>
    private static void EmitCreateContribution(
        TgmlClassDecl sourceClass, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var gmlName  = sourceClass.QualifiedName?.Replace(".", "_") ?? sourceClass.Name;

        // Field initialisers.
        foreach (var field in sourceClass.Fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _)) continue;

            var initVal = field.Initializer is not null
                ? exprEmit.Emit(field.Initializer)
                : exprEmit.Emit(new TgmlDefaultExpr { Type = field.Type });

            if (field.IsStatic)
                w.WriteLine($"global.{gmlName}_{field.Name} = {initVal};");
            else
                w.WriteLine($"{field.Name} = {initVal};");
        }

        // Backing fields for auto properties.
        foreach (var prop in sourceClass.Properties)
        {
            if (EmitHelpers.RequiresBackingField(prop, ctx))
                w.WriteLine($"__backing_{prop.Name} = undefined;");
        }

        // Instance methods (non-event, non-static).
        var instanceMethods = sourceClass.Methods
            .Where(m => !IsNativeEvent(sourceClass, m, ctx) && !m.IsStatic)
            .ToList();
        EmitInstanceMethods(sourceClass, instanceMethods, ctx, w);

        // Property accessors.
        EmitInstanceProperties(sourceClass.Properties, ctx, w);

        // Default (no-arg) constructor body is inlined into the Create event.
        var defaultCtor = sourceClass.Constructors.FirstOrDefault(c => c.Params.Count == 0);
        if (defaultCtor?.Body is { } ctorBody)
            stmtEmit.EmitBlock(ctorBody, w);

        // Named Create event method (if separately declared via @NativeEvent or override).
        if (TryGetCreateEventMethod(sourceClass, ctx, out var createMethod))
        {
            ctx.CurrentMethodName       = createMethod.Name;
            ctx.CurrentMethodIsOverride = createMethod.IsOverride;
            ctx.CurrentMethodOwnerType  = sourceClass;
            ctx.CurrentNativeEventName  = "Create";
            stmtEmit.EmitBlock(createMethod.Body!, w);
            ClearMethodContext(ctx);
        }
    }

    // ── Instance method emission ──────────────────────────────────────────────

    /// <summary>
    ///     Emits instance method assignments (<c>MethodName = function(...) { ... }</c>)
    ///     for all <paramref name="methods"/>, with overload dispatch when needed.
    /// </summary>
    private static void EmitInstanceMethods(
        TgmlClassDecl owner,
        IReadOnlyList<TgmlMethodDecl> methods,
        GenerationContext ctx,
        GmlWriter w)
    {
        if (methods.Count == 0) return;

        foreach (var group in methods
            .Where(m => !m.IsAbstract)
            .GroupBy(m => m.Name))
        {
            var overloads = group.ToList();
            w.WriteLine();

            if (overloads.Count == 1)
            {
                EmitInstanceMethodAs(overloads[0].Name, overloads[0], owner, ctx, w);
                continue;
            }

            for (var i = 0; i < overloads.Count; i++)
            {
                EmitInstanceMethodAs($"{group.Key}_{i}", overloads[i], owner, ctx, w);
                if (i < overloads.Count - 1) w.WriteLine();
            }

            // Runtime dispatcher.
            var dispatchOrder = overloads
                .Select((m, idx) => (Method: m, Idx: idx))
                .OrderByDescending(x => OverloadDispatchHelper.MethodSpecificity(x.Method))
                .ToList();

            w.WriteLine();
            w.WriteLine($"{group.Key} = function()");
            w.OpenBrace();
            for (var di = 0; di < dispatchOrder.Count; di++)
            {
                var (ov, origIdx) = dispatchOrder[di];
                var isLast   = di == dispatchOrder.Count - 1;
                var argExprs = string.Join(", ",
                    Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
                var ret = ov.ReturnType.Name.Full != "void" ? "return " : string.Empty;

                if (isLast)
                    w.WriteLine($"else {{ {ret}{group.Key}_{origIdx}({argExprs}); }}");
                else
                {
                    var cond    = OverloadDispatchHelper.BuildMethodDispatchCondition(ov, overloads);
                    var keyword = di == 0 ? "if" : "else if";
                    w.WriteLine($"{keyword} ({cond}) {{ {ret}{group.Key}_{origIdx}({argExprs}); }}");
                }
            }
            w.CloseBrace();
        }
    }

    /// <summary>
    ///     Emits a single instance method as <c>methodName = function(...) { ... }</c>,
    ///     honouring <c>NativeInstanceCallName</c> metadata for native-call stubs.
    /// </summary>
    private static void EmitInstanceMethodAs(
        string methodName, TgmlMethodDecl method, TgmlClassDecl declaringType,
        GenerationContext ctx, GmlWriter w)
    {
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));
        ctx.CurrentMethodName       = method.Name;
        ctx.CurrentMethodIsOverride = method.IsOverride;
        ctx.CurrentMethodOwnerType  = declaringType;
        ctx.CurrentNativeEventName  = null;

        if (method.Metadata.TryGetValue("NativeCallName", out var nco) && nco is string nativeCallName)
        {
            w.WriteLine($"{methodName} = function({paramStr}) {{ {nativeCallName}({paramStr}); }}");
            ClearMethodContext(ctx);
            return;
        }

        var stmtEmit = new StatementEmitter(ctx);
        w.WriteLine($"{methodName} = function({paramStr})");
        w.OpenBrace();
        if (method.Body is not null) stmtEmit.EmitBlock(method.Body, w);
        w.CloseBrace();
        ClearMethodContext(ctx);
    }

    // ── Property emission ─────────────────────────────────────────────────────

    /// <summary>Emits getter/setter instance functions for all non-asset properties.</summary>
    private static void EmitInstanceProperties(
        IReadOnlyList<TgmlPropertyDecl> properties, GenerationContext ctx, GmlWriter w)
    {
        foreach (var prop in properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _)) continue;
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: false);
        }
    }

    // ── Native event helpers ──────────────────────────────────────────────────

    private static bool IsNativeEvent(TgmlClassDecl owner, TgmlMethodDecl method, GenerationContext ctx)
        => TryGetNativeEventName(owner, method, ctx, out _);

    /// <summary>
    ///     Resolves the GameMaker event file name (e.g. <c>"Step"</c>) for <paramref name="method"/>
    ///     by checking <c>NativeEventName</c> metadata, known event names, and base-class resolution.
    /// </summary>
    private static bool TryGetNativeEventName(
        TgmlClassDecl owner, TgmlMethodDecl method, GenerationContext ctx, out string eventName)
    {
        eventName = string.Empty;

        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string named)
        {
            eventName = named;
            return true;
        }

        if (method.IsOverride && KnownEvents.Contains(method.Name))
        {
            eventName = method.Name;
            return true;
        }

        if (method.IsOverride &&
            ctx.TryFindBaseMethod(owner, method.Name, out _, out var baseMethod) &&
            TryGetNativeEventName(baseMethod, out eventName))
            return true;

        return false;
    }

    private static bool TryGetNativeEventName(TgmlMethodDecl method, out string eventName)
    {
        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string named)
        {
            eventName = named;
            return true;
        }

        if (method.IsOverride && KnownEvents.Contains(method.Name))
        {
            eventName = method.Name;
            return true;
        }

        eventName = string.Empty;
        return false;
    }

    private static bool TryGetCreateEventMethod(
        TgmlClassDecl owner, GenerationContext ctx, out TgmlMethodDecl method)
    {
        method = owner.Methods.FirstOrDefault(m =>
            m.Body is not null &&
            TryGetNativeEventName(owner, m, ctx, out var en) &&
            string.Equals(en, "Create", StringComparison.OrdinalIgnoreCase))!;
        return method is not null;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<TgmlExpression> GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor?.Metadata.TryGetValue("NormalizedBaseArgs", out var v) == true &&
            v is List<TgmlExpression> normalized)
            return normalized;
        return ctor?.BaseArgs?.Select(a => a.Value).ToList() ?? [];
    }

    private static void ClearMethodContext(GenerationContext ctx)
    {
        ctx.CurrentMethodName       = null;
        ctx.CurrentMethodIsOverride = false;
        ctx.CurrentMethodOwnerType  = null;
        ctx.CurrentNativeEventName  = null;
    }
}
