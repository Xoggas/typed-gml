using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits script classes and structs as self-contained GML constructor functions.
///     Every ancestor's members are inlined directly — GML has no prototype chain.
/// </summary>
/// <remarks>
///     <para>
///         <b>Method overloading:</b> N overloads are emitted as <c>static Method_0 … Method_N</c>
///         plus a runtime dispatcher that uses <c>argument_count</c> and GML type checks.
///     </para>
///     <para>
///         <b>Constructor overloading:</b> all overloads share ONE GML constructor function;
///         arguments are accessed via <c>argument[]</c> and dispatched at runtime.
///     </para>
///     <para>
///         <b>Synthesized members:</b> every type gets a default <c>static ToString</c> returning
///         its qualified name and a <c>static GetType</c> returning the <c>__TYPE_X</c> macro.
///         A user-defined <c>ToString</c> overrides the default (last-writer-wins in GML).
///     </para>
/// </remarks>
public sealed class ScriptClassEmitter : ICodeEmitter
{
    /// <inheritdoc/>
    public bool CanEmit(TgmlTypeDecl decl) =>
        (decl is TgmlClassDecl cls && !cls.IsGameObject) || decl is TgmlStructDecl;

    /// <inheritdoc/>
    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        ctx.CurrentType = decl;

        var (fields, properties, methods, constructors, _, typeParams, name) = decl switch
        {
            TgmlClassDecl c => (c.Fields, c.Properties, c.Methods, c.Constructors, c.BaseTypes, c.TypeParams, c.Name),
            TgmlStructDecl s => (s.Fields, s.Properties, s.Methods, s.Constructors, s.BaseTypes, s.TypeParams, s.Name),
            _ => throw new InvalidOperationException($"ScriptClassEmitter cannot emit {decl.GetType().Name}.")
        };

        var gmlName           = decl.QualifiedName?.Replace(".", "_") ?? name;
        var qualifiedTypeName = decl.QualifiedName ?? name;
        var stmtEmit          = new StatementEmitter(ctx);
        var exprEmit          = new ExpressionEmitter(ctx);
        var w                 = new GmlWriter();

        // Operator helper functions are file-level (outside the constructor).
        EmitUserDefinedOperatorHelpers(decl, methods, ctx, w);
        if (methods.Any(m => m.IsUserDefinedOperator))
            w.WriteLine();

        var typeIds       = EmitHelpers.BuildTypesStruct(decl, ctx);
        var typeArgParams = Enumerable.Range(0, typeParams.Count).Select(i => $"__t{i}").ToList();
        int typeArgOffset = typeArgParams.Count;

        if (constructors.Count <= 1)
            EmitSingleCtorBody(decl, gmlName, qualifiedTypeName,
                fields, properties, methods, constructors,
                typeIds, typeArgParams, ctx, exprEmit, stmtEmit, w);
        else
            EmitMultiCtorBody(decl, gmlName, qualifiedTypeName,
                fields, properties, methods, constructors,
                typeIds, typeArgParams, typeArgOffset, ctx, exprEmit, stmtEmit, w);

        yield return new GeneratedFile($"Scripts/{gmlName}/{gmlName}.gml", w.ToString());
    }

    // ── Single-constructor path ───────────────────────────────────────────────

    /// <summary>
    ///     Emits the GML constructor for a type with zero or one constructor declarations.
    ///     Ancestor members are inlined root-first, followed by own members and the ctor body.
    /// </summary>
    private static void EmitSingleCtorBody(
        TgmlTypeDecl decl, string gmlName, string qualifiedTypeName,
        List<TgmlFieldDecl> fields, List<TgmlPropertyDecl> properties,
        List<TgmlMethodDecl> methods, List<TgmlConstructorDecl> constructors,
        string typeIds, List<string> typeArgParams,
        GenerationContext ctx, ExpressionEmitter exprEmit, StatementEmitter stmtEmit, GmlWriter w)
    {
        var ctor       = constructors.FirstOrDefault();
        var ctorParams = ctor?.Params ?? [];
        var allParams  = typeArgParams.Concat(ctorParams.Select(p => p.Name));

        w.WriteLine($"function {gmlName}({string.Join(", ", allParams)}) constructor");
        w.OpenBrace();
        w.WriteLine($"__types = {{ {typeIds} }};");

        if (typeArgParams.Count > 0)
            w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParams)}];");

        // Synthesised default — user/ancestor overrides emitted later will overwrite.
        w.WriteLine($"static ToString = function() {{ return \"{qualifiedTypeName}\"; }};");

        // Bind ancestor constructor parameters, then inline bodies root → parent.
        var ancestorChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(ctor), ctx.TypeTable);

        foreach (var (ancestor, baseArgs) in ancestorChain)
        {
            if (baseArgs is null) continue;
            var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
            if (matchedCtor is null) continue;
            for (var i = 0; i < Math.Min(matchedCtor.Params.Count, baseArgs.Count); i++)
            {
                var argStr = exprEmit.Emit(baseArgs[i]);
                if (argStr != matchedCtor.Params[i].Name)
                    w.WriteLine($"var {matchedCtor.Params[i].Name} = {argStr};");
            }
        }

        foreach (var (ancestor, aBaseArgs) in Enumerable.Reverse(ancestorChain))
            EmitAncestorContribution(ancestor, aBaseArgs, ctx, exprEmit, stmtEmit, w);

        // Own fields, backing fields and ctor body.
        EmitFields(fields, gmlName, exprEmit, w, decl is TgmlStructDecl);
        EmitBackingFields(properties, exprEmit, ctx, w);

        if (ctor?.Body is { } ctorBody)
        {
            ctx.PushLocalScope(ctorParams.Select(p => p.Name));
            stmtEmit.EmitBlock(ctorBody, w);
            ctx.PopLocalScope();
        }

        // Static methods — user ToString overrides the synthesised default above.
        EmitOverloadedMethods(methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);

        foreach (var prop in properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }

        // GetType is last so it cannot be overridden.
        w.WriteLine();
        w.WriteLine($"static GetType = function() {{ return __TYPE_{gmlName}; }};");

        w.CloseBrace();
    }

    // ── Multi-constructor path ────────────────────────────────────────────────

    /// <summary>
    ///     Emits the GML constructor for a type with multiple constructor declarations.
    ///     A single GML function uses <c>argument[]</c> / <c>argument_count</c> to dispatch.
    /// </summary>
    private static void EmitMultiCtorBody(
        TgmlTypeDecl decl, string gmlName, string qualifiedTypeName,
        List<TgmlFieldDecl> fields, List<TgmlPropertyDecl> properties,
        List<TgmlMethodDecl> methods, List<TgmlConstructorDecl> constructors,
        string typeIds, List<string> typeArgParams, int typeArgOffset,
        GenerationContext ctx, ExpressionEmitter exprEmit, StatementEmitter stmtEmit, GmlWriter w)
    {
        w.WriteLine($"function {gmlName}({string.Join(", ", typeArgParams)}) constructor");
        w.OpenBrace();
        w.WriteLine($"__types = {{ {typeIds} }};");

        if (typeArgParams.Count > 0)
            w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParams)}];");

        w.WriteLine($"static ToString = function() {{ return \"{qualifiedTypeName}\"; }};");

        // Shared ancestor members (same regardless of which overload is invoked).
        var sharedChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(constructors[0]), ctx.TypeTable);
        foreach (var (ancestor, _) in Enumerable.Reverse(sharedChain))
        {
            var aGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
            EmitFields(ancestor.Fields, aGmlName, exprEmit, w, isStruct: false);
            EmitBackingFields(ancestor.Properties, exprEmit, ctx, w);
            EmitOverloadedMethods(ancestor.Methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);
            foreach (var prop in ancestor.Properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
            {
                w.WriteLine();
                PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
            }
        }

        EmitFields(fields, gmlName, exprEmit, w, decl is TgmlStructDecl);
        EmitBackingFields(properties, exprEmit, ctx, w);

        // Per-overload dispatch — most-specific first, fallback (else) last.
        var dispatchOrder = constructors
            .Select((c, idx) => (Ctor: c, Idx: idx))
            .OrderByDescending(x => OverloadDispatchHelper.CtorSpecificity(x.Ctor))
            .ToList();

        for (var di = 0; di < dispatchOrder.Count; di++)
        {
            var (overload, _) = dispatchOrder[di];
            var isLast        = di == dispatchOrder.Count - 1;
            var keyword       = di == 0 ? "if" : isLast ? "else" : "else if";
            var condition     = isLast ? string.Empty
                : $" ({OverloadDispatchHelper.BuildCtorDispatchCondition(overload, constructors, typeArgOffset)})";

            w.WriteLine($"{keyword}{condition}");
            w.OpenBrace();

            for (var pi = 0; pi < overload.Params.Count; pi++)
                w.WriteLine($"var {overload.Params[pi].Name} = argument[{pi + typeArgOffset}];");

            var overloadChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(overload), ctx.TypeTable);

            foreach (var (ancestor, aBaseArgs) in overloadChain)
            {
                if (aBaseArgs is null) continue;
                var matchedCtor = FindMatchingConstructor(ancestor, aBaseArgs);
                if (matchedCtor is null) continue;
                for (var pi = 0; pi < Math.Min(matchedCtor.Params.Count, aBaseArgs.Count); pi++)
                {
                    var argStr = exprEmit.Emit(aBaseArgs[pi]);
                    if (argStr != matchedCtor.Params[pi].Name)
                        w.WriteLine($"var {matchedCtor.Params[pi].Name} = {argStr};");
                }
            }

            foreach (var (ancestor, aBaseArgs) in Enumerable.Reverse(overloadChain))
            {
                var matchedCtor = FindMatchingConstructor(ancestor, aBaseArgs);
                if (matchedCtor?.Body is { } aBody)
                    stmtEmit.EmitBlock(aBody, w);
            }

            if (overload.Body is { } ctorBody)
            {
                ctx.PushLocalScope(overload.Params.Select(p => p.Name));
                stmtEmit.EmitBlock(ctorBody, w);
                ctx.PopLocalScope();
            }

            w.CloseBrace();
        }

        EmitOverloadedMethods(methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);
        foreach (var prop in properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }

        w.WriteLine();
        w.WriteLine($"static GetType = function() {{ return __TYPE_{gmlName}; }};");

        w.CloseBrace();
    }

    // ── Ancestor inlining ─────────────────────────────────────────────────────

    /// <summary>
    ///     Collects the transitive ancestor chain in direct-parent-first order.
    ///     <paramref name="ownBaseArgs"/> are the expressions the current type's constructor
    ///     passes to <c>base(...)</c>.
    /// </summary>
    private static List<(TgmlClassDecl Decl, List<TgmlExpression>? BaseArgs)> CollectAncestorChain(
        TgmlTypeDecl decl, List<TgmlExpression>? ownBaseArgs, TypeTable typeTable)
    {
        var chain           = new List<(TgmlClassDecl, List<TgmlExpression>?)>();
        var currentBases    = GetBaseTypeList(decl);
        var currentBaseArgs = ownBaseArgs;

        while (currentBases is not null)
        {
            TgmlClassDecl? parent = null;
            foreach (var bt in currentBases)
            {
                typeTable.TryResolve(bt.Name.Full, out var btDecl);
                if (btDecl is TgmlClassDecl pc) { parent = pc; break; }
            }
            if (parent is null) break;

            chain.Add((parent, currentBaseArgs));
            var matchedCtor = FindMatchingConstructor(parent, currentBaseArgs);
            currentBaseArgs = GetNormalizedBaseArgs(matchedCtor);
            currentBases    = parent.BaseTypes;
        }

        return chain;
    }

    /// <summary>
    ///     Inlines an ancestor's fields, backing fields, constructor body, static methods
    ///     and property accessors into the emitting constructor.
    /// </summary>
    private static void EmitAncestorContribution(
        TgmlClassDecl ancestor, List<TgmlExpression>? baseArgs,
        GenerationContext ctx, ExpressionEmitter exprEmit, StatementEmitter stmtEmit, GmlWriter w)
    {
        var aGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
        EmitFields(ancestor.Fields, aGmlName, exprEmit, w, isStruct: false);
        EmitBackingFields(ancestor.Properties, exprEmit, ctx, w);

        var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
        if (matchedCtor?.Body is { } body)
            stmtEmit.EmitBlock(body, w);

        EmitOverloadedMethods(ancestor.Methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);

        foreach (var prop in ancestor.Properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }
    }

    // ── Field emitters ────────────────────────────────────────────────────────

    /// <summary>Emits instance (<c>self.X</c>) and static (<c>global.Owner_X</c>) field initialisers.</summary>
    private static void EmitFields(
        IReadOnlyList<TgmlFieldDecl> fields, string ownerGmlName,
        ExpressionEmitter exprEmit, GmlWriter w, bool isStruct)
    {
        foreach (var field in fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _)) continue;

            var initVal = field.Initializer is not null
                ? exprEmit.Emit(field.Initializer)
                : EmitHelpers.DefaultStorageValue(field.Type, exprEmit);

            if (field.IsStatic)
                w.WriteLine($"global.{ownerGmlName}_{field.Name} = {initVal};");
            else
                w.WriteLine($"self.{field.Name} = {initVal};");
        }
    }

    /// <summary>Emits <c>__backing_PropName = default;</c> for properties that need a backing field.</summary>
    private static void EmitBackingFields(
        IReadOnlyList<TgmlPropertyDecl> properties,
        ExpressionEmitter exprEmit, GenerationContext ctx, GmlWriter w)
    {
        foreach (var prop in properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _)) continue;
            if (EmitHelpers.RequiresBackingField(prop, ctx))
                w.WriteLine($"__backing_{prop.Name} = {EmitHelpers.DefaultStorageValue(prop.Type, exprEmit)};");
        }
    }

    // ── Static method emission ────────────────────────────────────────────────

    /// <summary>
    ///     Groups methods by name and emits them as GML <c>static</c> function assignments.
    ///     Multiple overloads get numbered variants plus a runtime-dispatch wrapper.
    /// </summary>
    private static void EmitOverloadedMethods(
        IReadOnlyList<TgmlMethodDecl> methods, GenerationContext ctx, GmlWriter w)
    {
        if (methods.Count == 0) return;

        foreach (var group in methods
            .Where(m => !m.IsAbstract && !m.IsUserDefinedOperator)
            .GroupBy(m => m.Name))
        {
            var overloads = group.ToList();
            w.WriteLine();

            if (overloads.Count == 1)
            {
                EmitStaticMethod(overloads[0], ctx, w);
                continue;
            }

            for (var i = 0; i < overloads.Count; i++)
            {
                EmitStaticMethodAs($"{group.Key}_{i}", overloads[i], ctx, w);
                if (i < overloads.Count - 1) w.WriteLine();
            }

            // Dispatcher — most-specific overload tested first.
            var dispatchOrder = overloads
                .Select((m, idx) => (Method: m, Idx: idx))
                .OrderByDescending(x => OverloadDispatchHelper.MethodSpecificity(x.Method))
                .ToList();

            w.WriteLine();
            w.WriteLine($"static {group.Key} = function()");
            w.OpenBrace();
            for (var di = 0; di < dispatchOrder.Count; di++)
            {
                var (ov, origIdx) = dispatchOrder[di];
                var isLast   = di == dispatchOrder.Count - 1;
                var argExprs = string.Join(", ",
                    Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
                var ret = ov.ReturnType.Name.Full != "void" ? "return " : "";

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

    private static void EmitStaticMethod(TgmlMethodDecl method, GenerationContext ctx, GmlWriter w)
        => EmitStaticMethodAs(method.Name, method, ctx, w);

    /// <summary>
    ///     Emits a single <c>static gmlName = function(...) { ... };</c>,
    ///     honouring <c>NativeCallName</c> metadata for BCL native-call stubs.
    /// </summary>
    private static void EmitStaticMethodAs(
        string gmlName, TgmlMethodDecl method, GenerationContext ctx, GmlWriter w)
    {
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));

        if (method.Metadata.TryGetValue("NativeCallName", out var nco) && nco is string nativeCallName)
        {
            w.WriteLine($"static {gmlName} = function({paramStr}) {{ {nativeCallName}({paramStr}); }}");
            return;
        }

        var stmtEmit = new StatementEmitter(ctx);
        w.WriteLine($"static {gmlName} = function({paramStr})");
        w.OpenBrace();
        ctx.PushLocalScope(method.Params.Select(p => p.Name));
        if (method.Body is not null) stmtEmit.EmitBlock(method.Body, w);
        ctx.PopLocalScope();
        w.CloseBrace();
    }

    // ── User-defined operator helpers ─────────────────────────────────────────

    /// <summary>
    ///     Emits file-level GML helper functions for every user-defined operator/conversion.
    ///     These must be outside the constructor because GML operator helpers are plain functions.
    /// </summary>
    private static void EmitUserDefinedOperatorHelpers(
        TgmlTypeDecl owner, IReadOnlyList<TgmlMethodDecl> methods, GenerationContext ctx, GmlWriter w)
    {
        var helpers = methods.Where(m => m.IsUserDefinedOperator && m.Body is not null).ToList();
        if (helpers.Count == 0) return;

        // Save and restore emission context around operator helpers.
        var saved = (ctx.CurrentType, ctx.CurrentMethodName, ctx.CurrentMethodIsOverride,
                     ctx.CurrentMethodOwnerType, ctx.CurrentNativeEventName);

        ctx.CurrentType            = owner;
        ctx.CurrentNativeEventName = null;
        ctx.CurrentMethodOwnerType = owner as TgmlClassDecl;

        var stmtEmit = new StatementEmitter(ctx);
        foreach (var method in helpers)
        {
            ctx.CurrentMethodName       = method.Name;
            ctx.CurrentMethodIsOverride = method.IsOverride;

            var helperName = OperatorFacts.GetHelperName(owner, method);
            var paramStr   = string.Join(", ", method.Params.Select(p => p.Name));
            w.WriteLine($"function {helperName}({paramStr})");
            w.OpenBrace();
            stmtEmit.EmitBlock(method.Body!, w);
            w.CloseBrace();
            w.WriteLine();
        }

        (ctx.CurrentType, ctx.CurrentMethodName, ctx.CurrentMethodIsOverride,
         ctx.CurrentMethodOwnerType, ctx.CurrentNativeEventName) = saved;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Returns <c>true</c> for methods that are always compiler-synthesized.
    ///     Currently only <c>GetType</c> — its body is replaced by a <c>__TYPE_X</c> macro return.
    /// </summary>
    private static bool IsCompilerSynthesized(TgmlMethodDecl m) => m.Name is "GetType";

    private static List<TgmlTypeRef>? GetBaseTypeList(TgmlTypeDecl decl) => decl switch
    {
        TgmlClassDecl c  => c.BaseTypes,
        TgmlStructDecl s => s.BaseTypes,
        _ => null
    };

    /// <summary>
    ///     Returns the normalized base constructor args set by the checker, or falls back to
    ///     the raw parsed <see cref="TgmlConstructorDecl.BaseArgs"/>.
    /// </summary>
    private static List<TgmlExpression>? GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor is null) return null;
        if (ctor.Metadata.TryGetValue("NormalizedBaseArgs", out var v) && v is List<TgmlExpression> n)
            return n;
        return ctor.BaseArgs?.Select(a => a.Value).ToList();
    }

    /// <summary>
    ///     Picks the constructor of <paramref name="ancestor"/> whose parameter count matches
    ///     <paramref name="baseArgs"/>; falls back to the first constructor when ambiguous.
    /// </summary>
    private static TgmlConstructorDecl? FindMatchingConstructor(
        TgmlClassDecl ancestor, List<TgmlExpression>? baseArgs)
    {
        if (ancestor.Constructors.Count == 0) return null;
        if (baseArgs is null)                 return ancestor.Constructors[0];

        var byCount = ancestor.Constructors.Where(c => c.Params.Count == baseArgs.Count).ToList();
        return byCount.Count == 1 ? byCount[0] : ancestor.Constructors[0];
    }
}

