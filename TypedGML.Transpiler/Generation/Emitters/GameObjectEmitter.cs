using TypedGML.Transpiler.Population.Models;

using TypedGML.Transpiler.Checking;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits @Object-decorated classes as GameMaker object event scripts.
///     Per @Object class produces:
///     Objects/obj_Name/Create_0.gml       — field defaults + __types + instance methods + ctor body
///     Objects/obj_Name/EventName_0.gml    — for each @NativeEvent method or override method
///     Scripts/obj_Name_Init/...gml        — if ctor has extra args beyond base() args
/// </summary>
public sealed class GameObjectEmitter : ICodeEmitter
{
    // Known GML event method names → event file prefixes
    private static readonly HashSet<string> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Create", "Destroy", "Step", "Draw", "Alarm", "Collision",
        "KeyDown", "KeyUp", "KeyPressed", "MouseDown", "MouseUp",
        "MouseMoved", "BeginStep", "EndStep", "DrawGui", "PreDraw",
        "PostDraw", "RoomStart", "RoomEnd", "GameStart", "GameEnd",
        "CleanUp", "AsyncSystem", "AsyncHttp", "AsyncSaving"
    };

    public bool CanEmit(TgmlTypeDecl decl)
    {
        return decl is TgmlClassDecl cls && cls.IsGameObject;
    }

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var cls = (TgmlClassDecl)decl;
        ctx.CurrentType = cls;
        var objName = ctx.GmlObjectName(cls); // e.g. "obj_Player"
        var gmlName = cls.QualifiedName?.Replace(".", "_") ?? cls.Name;
        var stmtEmit = new StatementEmitter(ctx);
        var createWriter = new GmlWriter();
        var eventFiles = new List<GeneratedFile>();

        // ── Create event ─────────────────────────────────────────────────────
        {
            var typeIds = BuildTypesStruct(cls, ctx);
            createWriter.WriteLine($"// Create event for {objName}");
            createWriter.WriteLine($"__types = {{ {typeIds} }};");
        }

        foreach (var ancestor in ctx.GetClassAncestorChain(cls))
        {
            EmitCreateContribution(ancestor, ctx, createWriter);
        }

        EmitCreateContribution(cls, ctx, createWriter);

        // ── Native event scripts ──────────────────────────────────────────────
        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract || method.Body is null || !TryGetNativeEventName(cls, method, ctx, out var eventName))
            {
                continue;
            }

            if (!string.Equals(eventName, "Create", StringComparison.OrdinalIgnoreCase))
            {
                var w = new GmlWriter();
                ctx.CurrentMethodName = method.Name;
                ctx.CurrentMethodIsOverride = method.IsOverride;
                ctx.CurrentMethodOwnerType = cls;
                ctx.CurrentNativeEventName = eventName;
                stmtEmit.EmitBlock(method.Body, w);
                ClearMethodContext(ctx);
                eventFiles.Add(new GeneratedFile($"Objects/{objName}/{eventName}_0.gml", w.ToString()));
            }
        }

        yield return new GeneratedFile($"Objects/{objName}/Create_0.gml", createWriter.ToString());

        foreach (var eventFile in eventFiles)
            yield return eventFile;

        // ── Init script (extra constructor params beyond base args) ───────────
        if (cls.Constructor is { } ctor)
        {
            var baseArgCount = GetNormalizedBaseArgs(ctor).Count;
            var extraParams = ctor.Params.Skip(baseArgCount).ToList();

            if (extraParams.Count > 0)
            {
                var initScriptName = $"{objName}_Init";
                var w = new GmlWriter();
                var paramStr = string.Join(", ", new[] { "inst" }.Concat(extraParams.Select(p => p.Name)));
                w.WriteLine($"function {initScriptName}({paramStr})");
                w.OpenBrace();
                w.WriteLine("with (inst)");
                w.OpenBrace();
                stmtEmit.EmitBlock(ctor.Body, w);
                w.CloseBrace();
                w.CloseBrace();
                yield return new GeneratedFile($"Scripts/{initScriptName}/{initScriptName}.gml", w.ToString());
            }
        }
    }

    private static bool RequiresBackingField(TgmlPropertyDecl prop, GenerationContext ctx)
    {
        if (prop.IsIndexer)
            return false;

        if (AssetFacts.TryGetAssetName(prop, out _))
            return false;

        return (prop.Getter?.IsAuto == true || prop.Setter?.IsAuto == true) &&
               ctx.GetNativePropertyName(prop) is null;
    }

    private static bool IsNativeEvent(TgmlClassDecl owner, TgmlMethodDecl method, GenerationContext ctx)
    {
        return TryGetNativeEventName(owner, method, ctx, out _);
    }

    private static bool TryGetNativeEventName(TgmlClassDecl owner, TgmlMethodDecl method, GenerationContext ctx, out string eventName)
    {
        eventName = string.Empty;

        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string namedEvent)
        {
            eventName = namedEvent;
            return true;
        }

        if (method.IsOverride && KnownEvents.Contains(method.Name))
        {
            eventName = method.Name;
            return true;
        }

        if (method.IsOverride && ctx.TryFindBaseMethod(owner, method.Name, out _, out var baseMethod) &&
            TryGetNativeEventName(baseMethod, out eventName))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetNativeEventName(TgmlMethodDecl method, out string eventName)
    {
        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string namedEvent)
        {
            eventName = namedEvent;
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

    private static void EmitCreateContribution(
        TgmlClassDecl sourceClass,
        GenerationContext ctx,
        GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var gmlName = sourceClass.QualifiedName?.Replace(".", "_") ?? sourceClass.Name;

        foreach (var field in sourceClass.Fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _))
                continue;

            if (field.IsStatic)
            {
                var globalName = $"global.{gmlName}_{field.Name}";
                var initVal = field.Initializer is not null
                    ? exprEmit.Emit(field.Initializer)
                    : "undefined";
                w.WriteLine($"{globalName} = {initVal};");
            }
            else
            {
                var initVal = field.Initializer is not null
                    ? exprEmit.Emit(field.Initializer)
                    : "undefined";
                w.WriteLine($"{field.Name} = {initVal};");
            }
        }

        foreach (var prop in sourceClass.Properties)
        {
            if (RequiresBackingField(prop, ctx))
                w.WriteLine($"__backing_{prop.Name} = undefined;");
        }

        EmitInstanceMethods(sourceClass, sourceClass.Methods.Where(m => !IsNativeEvent(sourceClass, m, ctx)).ToList(), ctx, w);
        EmitInstanceProperties(sourceClass.Properties, ctx, w);

        if (sourceClass.Constructor?.Body is { } ctorBody)
            stmtEmit.EmitBlock(ctorBody, w);

        if (TryGetCreateEventMethod(sourceClass, ctx, out var createMethod))
        {
            ctx.CurrentMethodName = createMethod.Name;
            ctx.CurrentMethodIsOverride = createMethod.IsOverride;
            ctx.CurrentMethodOwnerType = sourceClass;
            ctx.CurrentNativeEventName = "Create";
            stmtEmit.EmitBlock(createMethod.Body!, w);
            ClearMethodContext(ctx);
        }
    }

    private static bool TryGetCreateEventMethod(TgmlClassDecl owner, GenerationContext ctx, out TgmlMethodDecl method)
    {
        method = owner.Methods.FirstOrDefault(m =>
            m.Body is not null &&
            TryGetNativeEventName(owner, m, ctx, out var eventName) &&
            string.Equals(eventName, "Create", StringComparison.OrdinalIgnoreCase))!;

        return method is not null;
    }

    private static void EmitInstanceMethods(TgmlClassDecl owner, List<TgmlMethodDecl> methods, GenerationContext ctx, GmlWriter w)
    {
        if (methods.Count == 0) return;

        var groups = methods
            .Where(m => !m.IsAbstract)
            .GroupBy(m => m.Name)
            .ToList();

        foreach (var group in groups)
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

            var dispatchOrder = overloads
                .Select((m, idx) => (Method: m, Idx: idx))
                .OrderByDescending(x => MethodSpecificity(x.Method))
                .ToList();

            w.WriteLine();
            w.WriteLine($"{group.Key} = function()");
            w.OpenBrace();
            for (var di = 0; di < dispatchOrder.Count; di++)
            {
                var (ov, origIdx) = dispatchOrder[di];
                var isLast = di == dispatchOrder.Count - 1;
                var argExprs = string.Join(", ",
                    Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
                var ret = ov.ReturnType.Name.Full != "void" ? "return " : string.Empty;

                if (isLast)
                {
                    w.WriteLine($"else {{ {ret}{group.Key}_{origIdx}({argExprs}); }}");
                }
                else
                {
                    var cond = BuildMethodDispatchCondition(ov, overloads);
                    var keyword = di == 0 ? "if" : "else if";
                    w.WriteLine($"{keyword} ({cond}) {{ {ret}{group.Key}_{origIdx}({argExprs}); }}");
                }
            }
            w.CloseBrace();
        }
    }

    private static void EmitInstanceMethodAs(string methodName, TgmlMethodDecl method, TgmlClassDecl declaringType, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));
        ctx.CurrentMethodName = method.Name;
        ctx.CurrentMethodIsOverride = method.IsOverride;
        ctx.CurrentMethodOwnerType = declaringType;
        ctx.CurrentNativeEventName = null;
        w.WriteLine($"{methodName} = function({paramStr})");
        w.OpenBrace();
        if (method.Body is not null) stmtEmit.EmitBlock(method.Body, w);
        w.CloseBrace();
        ClearMethodContext(ctx);
    }

    private static void EmitInstanceProperties(List<TgmlPropertyDecl> properties, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);

        foreach (var prop in properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _))
                continue;

            w.WriteLine();
            if (prop.IsIndexer)
            {
                EmitIndexer(prop, ctx, stmtEmit, w);
                continue;
            }

            ctx.CurrentPropertyName = prop.Name;
            var nativePropertyName = ctx.GetNativePropertyName(prop);

            if (prop.Getter is { } getter)
            {
                ctx.InsideGetter = true;
                ctx.InsideSetter = false;

                if (nativePropertyName is not null && getter.IsAuto)
                    w.WriteLine($"get_{prop.Name} = function() {{ return {nativePropertyName}; }}");
                else if (getter.IsAuto)
                    w.WriteLine($"get_{prop.Name} = function() {{ return __backing_{prop.Name}; }}");
                else
                {
                    w.WriteLine($"get_{prop.Name} = function()");
                    w.OpenBrace();
                    if (getter.Body is not null) stmtEmit.EmitBlock(getter.Body, w);
                    w.CloseBrace();
                }
            }

            if (prop.Setter is { } setter)
            {
                ctx.InsideGetter = false;
                ctx.InsideSetter = true;

                if (nativePropertyName is not null && setter.IsAuto)
                    w.WriteLine($"set_{prop.Name} = function(value) {{ {nativePropertyName} = value; }}");
                else if (setter.IsAuto)
                    w.WriteLine($"set_{prop.Name} = function(value) {{ __backing_{prop.Name} = value; }}");
                else
                {
                    w.WriteLine($"set_{prop.Name} = function(value)");
                    w.OpenBrace();
                    if (setter.Body is not null) stmtEmit.EmitBlock(setter.Body, w);
                    w.CloseBrace();
                }
            }

            ctx.InsideGetter = false;
            ctx.InsideSetter = false;
            ctx.CurrentPropertyName = null;
        }
    }

    private static void EmitIndexer(TgmlPropertyDecl prop, GenerationContext ctx, StatementEmitter stmtEmit, GmlWriter w)
    {
        var indexParamName = prop.IndexParam?.Name ?? "index";
        ctx.CurrentPropertyName = null;

        if (prop.Getter is { } getter)
        {
            ctx.InsideGetter = true;
            ctx.InsideSetter = false;
            w.WriteLine($"get_Item = function({indexParamName})");
            w.OpenBrace();
            if (getter.Body is not null)
                stmtEmit.EmitBlock(getter.Body, w);
            w.CloseBrace();
        }

        if (prop.Setter is { } setter)
        {
            ctx.InsideGetter = false;
            ctx.InsideSetter = true;
            w.WriteLine($"set_Item = function({indexParamName}, value)");
            w.OpenBrace();
            if (setter.Body is not null)
                stmtEmit.EmitBlock(setter.Body, w);
            w.CloseBrace();
        }

        ctx.InsideGetter = false;
        ctx.InsideSetter = false;
        ctx.CurrentPropertyName = null;
    }

    private static string BuildMethodDispatchCondition(
        TgmlMethodDecl overload, List<TgmlMethodDecl> all)
    {
        var count = overload.Params.Count;
        var sameCounts = all.Where(o => o != overload && o.Params.Count == count).ToList();
        var conditions = new List<string> { $"argument_count == {count}" };

        if (sameCounts.Count > 0)
            AppendTypeDistinguisher(conditions, overload.Params, sameCounts.Select(o => o.Params).ToList(), "argument");

        return string.Join(" && ", conditions);
    }

    private static void AppendTypeDistinguisher(
        List<string> conditions,
        IReadOnlyList<TgmlParam> myParams,
        List<List<TgmlParam>> otherParams,
        string argPrefix)
    {
        for (var i = 0; i < myParams.Count; i++)
        {
            var myType = myParams[i].Type.Name.Full;
            var conflictAtPos = otherParams.Any(o => o[i].Type.Name.Full == myType);
            if (conflictAtPos) continue;

            var check = GmlTypeCheck(myType, $"{argPrefix}[{i}]");
            if (check is not null) conditions.Add(check);
            break;
        }
    }

    private static int MethodSpecificity(TgmlMethodDecl method) =>
        method.Params.Sum(p => ParamTypeSpecificity(p.Type.Name.Full));

    private static int ParamTypeSpecificity(string tgmlType) =>
        tgmlType switch
        {
            "string" or "bool" or "array" => 3,
            "number" or "int" or "real" => 0,
            _ => 2
        };

    private static string? GmlTypeCheck(string tgmlType, string argExpr) =>
        tgmlType switch
        {
            "string" => $"is_string({argExpr})",
            "bool" => $"is_bool({argExpr})",
            "array" => $"is_array({argExpr})",
            "number" or "int" or "real" => null,
            _ => $"is_struct({argExpr})"
        };

    private static IReadOnlyList<TgmlExpression> GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor?.Metadata.TryGetValue("NormalizedBaseArgs", out var normalizedArgs) == true &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return ctor?.BaseArgs?.Select(a => a.Value).ToList() ?? [];
    }

    private static void ClearMethodContext(GenerationContext ctx)
    {
        ctx.CurrentMethodName = null;
        ctx.CurrentMethodIsOverride = false;
        ctx.CurrentMethodOwnerType = null;
        ctx.CurrentNativeEventName = null;
    }

    private static string BuildTypesStruct(TgmlClassDecl cls, GenerationContext ctx)
    {
        var names = TypeHierarchyHelper.CollectAllGmlTypeNames(cls, ctx.TypeTable);
        return string.Join(", ", names.Select(n => $"__TYPE_{n}: true"));
    }
}
