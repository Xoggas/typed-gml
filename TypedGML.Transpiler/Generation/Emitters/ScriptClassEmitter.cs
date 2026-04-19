using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits script classes as self-contained GML constructor functions.
///     Every ancestor's members are inlined directly — no GML prototype chain needed.
///
///     Method overloading: when N overloads share the same name, they are emitted as
///     <c>static Method_0</c>, <c>static Method_1</c>, … plus a runtime dispatcher
///     <c>static Method</c> that uses <c>argument_count</c> and GML type checks.
///
///     Constructor overloading: all overloads share ONE GML constructor function.
///     User arguments are accessed via <c>argument[]</c> and dispatched at runtime.
/// </summary>
public sealed class ScriptClassEmitter : ICodeEmitter
{
    public bool CanEmit(TgmlTypeDecl decl)
    {
        return (decl is TgmlClassDecl cls && !cls.IsGameObject) ||
               decl is TgmlStructDecl;
    }

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        ctx.CurrentType = decl;

        var (fields, properties, methods, constructors, baseTypes, typeParams, name) = decl switch
        {
            TgmlClassDecl c => (c.Fields, c.Properties, c.Methods, c.Constructors, c.BaseTypes, c.TypeParams, c.Name),
            TgmlStructDecl s => (s.Fields, s.Properties, s.Methods, s.Constructors, s.BaseTypes, s.TypeParams, s.Name),
            _ => throw new InvalidOperationException()
        };

        var gmlName = decl.QualifiedName?.Replace(".", "_") ?? name;
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var w = new GmlWriter();

        EmitUserDefinedOperatorHelpers(decl, methods, ctx, w);
        if (methods.Any(m => m.IsUserDefinedOperator))
            w.WriteLine();

        var typeIds = BuildTypesStruct(decl, ctx);
        var typeArgParamNames = Enumerable.Range(0, typeParams.Count).Select(i => $"__t{i}").ToList();
        int typeArgOffset = typeArgParamNames.Count;

        bool multiCtor = constructors.Count > 1;

        if (!multiCtor)
        {
            // ── Single (or no) constructor: classic inlined emission ──────────────
            var constructor = constructors.FirstOrDefault();
            var ctorParams = constructor?.Params ?? new List<TgmlParam>();
            var allParamNames = typeArgParamNames.Concat(ctorParams.Select(p => p.Name)).ToList();
            var paramStr = string.Join(", ", allParamNames);

            w.WriteLine($"function {gmlName}({paramStr}) constructor");
            w.OpenBrace();
            w.WriteLine($"__types = {{ {typeIds} }};");

            if (typeParams.Count > 0)
                w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParamNames)}];");

            var ancestorChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(constructor), ctx.TypeTable);

            // Ancestor param bindings (direct-parent → root order)
            foreach (var (ancestor, baseArgs) in ancestorChain)
            {
                if (baseArgs is null) continue;
                var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
                if (matchedCtor is null) continue;
                var aParams = matchedCtor.Params;
                for (var i = 0; i < Math.Min(aParams.Count, baseArgs.Count); i++)
                {
                    var argStr = exprEmit.Emit(baseArgs[i]);
                    if (argStr != aParams[i].Name)
                        w.WriteLine($"var {aParams[i].Name} = {argStr};");
                }
            }

            // Ancestor bodies root → parent
            foreach (var (ancestor, aBaseArgs) in Enumerable.Reverse(ancestorChain))
                EmitClassBody(ancestor, aBaseArgs, ctx, exprEmit, stmtEmit, w);

            // Own content
            EmitOwnFields(fields, gmlName, exprEmit, w, decl is TgmlStructDecl);

            foreach (var prop in properties)
            {
                if (AssetFacts.TryGetAssetName(prop, out _))
                    continue;

                if (RequiresBackingField(prop, ctx))
                    w.WriteLine($"__backing_{prop.Name} = {GetDefaultStorageValue(prop.Type, exprEmit, decl is TgmlStructDecl)};");
            }

            if (constructor?.Body is { } ctorBody)
                stmtEmit.EmitBlock(ctorBody, w);

            EmitOverloadedMethods(methods, ctx, w);

            foreach (var prop in properties)
            {
                if (AssetFacts.TryGetAssetName(prop, out _))
                    continue;

                w.WriteLine();
                EmitProperty(prop, ctx, w);
            }
        }
        else
        {
            // ── Multiple constructors: argument[] dispatch ────────────────────────
            // The GML function has no user-visible params; type-args are still named.
            var paramStr = string.Join(", ", typeArgParamNames);
            w.WriteLine($"function {gmlName}({paramStr}) constructor");
            w.OpenBrace();
            w.WriteLine($"__types = {{ {typeIds} }};");

            if (typeParams.Count > 0)
                w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParamNames)}];");

            // Shared ancestor fields/statics (any overload triggers same inheritance)
            var sharedChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(constructors[0]), ctx.TypeTable);
            foreach (var (ancestor, _) in Enumerable.Reverse(sharedChain))
            {
                var aGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
                EmitOwnFields(ancestor.Fields, aGmlName, exprEmit, w, false);
                foreach (var prop in ancestor.Properties)
                {
                    if (AssetFacts.TryGetAssetName(prop, out _))
                        continue;

                    if (RequiresBackingField(prop, ctx))
                        w.WriteLine($"__backing_{prop.Name} = undefined;");
                }
                EmitOverloadedMethods(ancestor.Methods, ctx, w);
                foreach (var prop in ancestor.Properties)
                {
                    if (AssetFacts.TryGetAssetName(prop, out _))
                        continue;

                    w.WriteLine();
                    EmitProperty(prop, ctx, w);
                }
            }

            // Own fields (shared, before dispatch)
            EmitOwnFields(fields, gmlName, exprEmit, w, decl is TgmlStructDecl);
            foreach (var prop in properties)
            {
                if (AssetFacts.TryGetAssetName(prop, out _))
                    continue;

                if (RequiresBackingField(prop, ctx))
                    w.WriteLine($"__backing_{prop.Name} = {GetDefaultStorageValue(prop.Type, exprEmit, decl is TgmlStructDecl)};");
            }

            // Per-overload dispatch (sorted: most specific first, fallback last)
            var ctorDispatch = constructors
                .Select((c, idx) => (Ctor: c, Idx: idx))
                .OrderByDescending(x => CtorSpecificity(x.Ctor))
                .ToList();

            for (var di = 0; di < ctorDispatch.Count; di++)
            {
                var (overload, oi) = ctorDispatch[di];
                var isLast = di == ctorDispatch.Count - 1;

                if (di == 0 && !isLast)
                {
                    w.WriteLine($"if ({BuildCtorDispatchCondition(overload, constructors, typeArgOffset)})");
                    w.OpenBrace();
                }
                else if (!isLast)
                {
                    w.WriteLine($"else if ({BuildCtorDispatchCondition(overload, constructors, typeArgOffset)})");
                    w.OpenBrace();
                }
                else
                {
                    w.WriteLine("else");
                    w.OpenBrace();
                }

                // Bind own params from argument[]
                for (var pi = 0; pi < overload.Params.Count; pi++)
                    w.WriteLine($"var {overload.Params[pi].Name} = argument[{pi + typeArgOffset}];");

                // Build ancestor chain for this specific overload's base args
                var overloadChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(overload), ctx.TypeTable);

                // Ancestor param bindings for this overload
                foreach (var (ancestor, aBaseArgs) in overloadChain)
                {
                    if (aBaseArgs is null) continue;
                    var matchedCtor = FindMatchingConstructor(ancestor, aBaseArgs);
                    if (matchedCtor is null) continue;
                    var aParams = matchedCtor.Params;
                    for (var pi = 0; pi < Math.Min(aParams.Count, aBaseArgs.Count); pi++)
                    {
                        var argStr = exprEmit.Emit(aBaseArgs[pi]);
                        if (argStr != aParams[pi].Name)
                            w.WriteLine($"var {aParams[pi].Name} = {argStr};");
                    }
                }

                // Ancestor constructor bodies (root → parent)
                foreach (var (ancestor, aBaseArgs) in Enumerable.Reverse(overloadChain))
                {
                    var matchedCtor = FindMatchingConstructor(ancestor, aBaseArgs);
                    if (matchedCtor?.Body is { } aBody)
                        stmtEmit.EmitBlock(aBody, w);
                }

                // Own constructor body
                if (overload.Body is { } ctorBody)
                    stmtEmit.EmitBlock(ctorBody, w);

                w.CloseBrace();
            }

            // Own statics and properties (shared, after dispatch)
            EmitOverloadedMethods(methods, ctx, w);
            foreach (var prop in properties)
            {
                if (AssetFacts.TryGetAssetName(prop, out _))
                    continue;

                w.WriteLine();
                EmitProperty(prop, ctx, w);
            }
        }

        w.CloseBrace();
        yield return new GeneratedFile($"Scripts/{gmlName}/{gmlName}.gml", w.ToString());
    }

    // ── Ancestor helpers ─────────────────────────────────────────────────────

    /// <summary>
    ///     Collects the inheritance chain starting from the direct parent of <paramref name="decl"/>.
    ///     <paramref name="ownBaseArgs"/> overrides the base-args supplied by the current type's
    ///     constructor (used to support per-overload ancestor inlining).
    /// </summary>
    private static List<(TgmlClassDecl Decl, List<TgmlExpression>? BaseArgs)> CollectAncestorChain(
        TgmlTypeDecl decl, List<TgmlExpression>? ownBaseArgs, TypeTable typeTable)
    {
        var chain = new List<(TgmlClassDecl, List<TgmlExpression>?)>();

        var currentBases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            _ => null
        };
        var currentBaseArgs = ownBaseArgs;

        while (currentBases is not null)
        {
            TgmlClassDecl? parentDecl = null;
            foreach (var bt in currentBases)
            {
                typeTable.TryResolve(bt.Name.Full, out var btDecl);
                if (btDecl is TgmlClassDecl pc) { parentDecl = pc; break; }
            }
            if (parentDecl is null) break;

            chain.Add((parentDecl, currentBaseArgs));

            // Find the constructor that matches the args we're passing (by count)
            var matchedCtor = FindMatchingConstructor(parentDecl, currentBaseArgs);
            currentBaseArgs = GetNormalizedBaseArgs(matchedCtor);
            currentBases = parentDecl.BaseTypes;
        }

        return chain;
    }

    /// <summary>
    ///     Picks the constructor of <paramref name="ancestor"/> that best matches
    ///     the supplied <paramref name="baseArgs"/> (matched by argument count).
    ///     Falls back to the first constructor when no unique match exists.
    /// </summary>
    private static TgmlConstructorDecl? FindMatchingConstructor(
        TgmlClassDecl ancestor, List<TgmlExpression>? baseArgs)
    {
        if (ancestor.Constructors.Count == 0) return null;
        if (baseArgs is null) return ancestor.Constructors[0];

        var byCount = ancestor.Constructors
            .Where(c => c.Params.Count == baseArgs.Count)
            .ToList();

        return byCount.Count == 1 ? byCount[0] : ancestor.Constructors[0];
    }

    private static List<TgmlExpression>? GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor is null)
            return null;

        if (ctor.Metadata.TryGetValue("NormalizedBaseArgs", out var normalizedArgs) &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return ctor.BaseArgs?.Select(a => a.Value).ToList();
    }

    /// <summary>
    ///     Inlines an ancestor's non-static fields, backing fields, constructor body,
    ///     static methods and property accessors into the currently-emitting constructor.
    /// </summary>
    private static void EmitClassBody(
        TgmlClassDecl ancestor,
        List<TgmlExpression>? baseArgs,
        GenerationContext ctx,
        ExpressionEmitter exprEmit,
        StatementEmitter stmtEmit,
        GmlWriter w)
    {
        var ancestorGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
        EmitOwnFields(ancestor.Fields, ancestorGmlName, exprEmit, w, false);

        foreach (var prop in ancestor.Properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _))
                continue;

            if (RequiresBackingField(prop, ctx))
                w.WriteLine($"__backing_{prop.Name} = undefined;");
        }

        var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
        if (matchedCtor?.Body is { } body)
            stmtEmit.EmitBlock(body, w);

        EmitOverloadedMethods(ancestor.Methods, ctx, w);

        foreach (var prop in ancestor.Properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _))
                continue;

            w.WriteLine();
            EmitProperty(prop, ctx, w);
        }
    }

    // ── Field, method, property emitters ─────────────────────────────────────

    private static void EmitOwnFields(
        List<TgmlFieldDecl> fields, string ownerGmlName,
        ExpressionEmitter exprEmit, GmlWriter w, bool useValueTypeDefaults)
    {
        foreach (var field in fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _))
                continue;

            if (field.IsStatic)
            {
                var globalName = $"global.{ownerGmlName}_{field.Name}";
                var initVal = field.Initializer is not null
                    ? exprEmit.Emit(field.Initializer)
                    : GetDefaultStorageValue(field.Type, exprEmit, useValueTypeDefaults);
                w.WriteLine($"{globalName} = {initVal};");
            }
            else
            {
                var initVal = field.Initializer is not null
                    ? exprEmit.Emit(field.Initializer)
                    : GetDefaultStorageValue(field.Type, exprEmit, useValueTypeDefaults);
                w.WriteLine($"self.{field.Name} = {initVal};");
            }
        }
    }

    private static string GetDefaultStorageValue(TgmlTypeRef type, ExpressionEmitter exprEmit, bool useValueTypeDefaults)
    {
        if (!useValueTypeDefaults)
            return "undefined";

        return exprEmit.Emit(new TgmlDefaultExpr { Type = type });
    }

    private static string BuildTypesStruct(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var names = TypeHierarchyHelper.CollectAllGmlTypeNames(decl, ctx.TypeTable);
        return string.Join(", ", names.Select(n => $"__TYPE_{n}: true"));
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

    // ── Overloaded method emission ────────────────────────────────────────────

    /// <summary>
    ///     Groups <paramref name="methods"/> by name. Single-method groups are emitted normally.
    ///     Multi-method groups get numbered variants (<c>Name_0</c>, <c>Name_1</c>, …) plus
    ///     a runtime dispatcher (<c>Name</c>) that uses <c>argument_count</c> and GML type checks.
    /// </summary>
    private static void EmitOverloadedMethods(
        List<TgmlMethodDecl> methods, GenerationContext ctx, GmlWriter w)
    {
        if (methods.Count == 0) return;

        var groups = methods
            .Where(m => !m.IsAbstract && !m.IsUserDefinedOperator)
            .GroupBy(m => m.Name)
            .ToList();

        foreach (var group in groups)
        {
            var overloads = group.ToList();
            w.WriteLine();

            if (overloads.Count == 1)
            {
                EmitStaticMethod(overloads[0], ctx, w);
            }
            else
            {
                // Numbered overloads (declaration order)
                for (var i = 0; i < overloads.Count; i++)
                {
                    EmitStaticMethodAs($"{group.Key}_{i}", overloads[i], ctx, w);
                    if (i < overloads.Count - 1) w.WriteLine();
                }

                // Dispatcher: sort by specificity so the most type-checkable overloads
                // are tested first, leaving the least-specific numeric fallback last.
                var dispatchOrder = overloads
                    .Select((m, idx) => (Method: m, Idx: idx))
                    .OrderByDescending(x => MethodSpecificity(x.Method))
                    .ToList();

                w.WriteLine();
                w.WriteLine($"static {group.Key} = function()");
                w.OpenBrace();
                for (var di = 0; di < dispatchOrder.Count; di++)
                {
                    var (ov, origIdx) = dispatchOrder[di];
                    var isLast = di == dispatchOrder.Count - 1;
                    var argExprs = string.Join(", ",
                        Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
                    var ret = ov.ReturnType.Name.Full != "void" ? "return " : "";

                    if (isLast)
                    {
                        // Fallback: use else { } so void methods don't fall through from earlier branches
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
    }

    private static void EmitStaticMethod(TgmlMethodDecl method, GenerationContext ctx, GmlWriter w)
        => EmitStaticMethodAs(method.Name, method, ctx, w);

    private static void EmitStaticMethodAs(
        string gmlName, TgmlMethodDecl method, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));
        w.WriteLine($"static {gmlName} = function({paramStr})");
        w.OpenBrace();
        if (method.Body is not null) stmtEmit.EmitBlock(method.Body, w);
        w.CloseBrace();
    }

    private static void EmitUserDefinedOperatorHelpers(
        TgmlTypeDecl owner,
        IReadOnlyList<TgmlMethodDecl> methods,
        GenerationContext ctx,
        GmlWriter w)
    {
        var helpers = methods.Where(m => m.IsUserDefinedOperator && m.Body is not null).ToList();
        if (helpers.Count == 0)
            return;

        var stmtEmit = new StatementEmitter(ctx);
        var previousCurrentType = ctx.CurrentType;
        var previousMethodName = ctx.CurrentMethodName;
        var previousMethodIsOverride = ctx.CurrentMethodIsOverride;
        var previousMethodOwnerType = ctx.CurrentMethodOwnerType;
        var previousNativeEventName = ctx.CurrentNativeEventName;

        ctx.CurrentType = owner;
        ctx.CurrentNativeEventName = null;
        ctx.CurrentMethodOwnerType = owner as TgmlClassDecl;

        foreach (var method in helpers)
        {
            ctx.CurrentMethodName = method.Name;
            ctx.CurrentMethodIsOverride = method.IsOverride;

            var helperName = OperatorFacts.GetHelperName(owner, method);
            var paramStr = string.Join(", ", method.Params.Select(p => p.Name));
            w.WriteLine($"function {helperName}({paramStr})");
            w.OpenBrace();
            stmtEmit.EmitBlock(method.Body!, w);
            w.CloseBrace();
            w.WriteLine();
        }

        ctx.CurrentType = previousCurrentType;
        ctx.CurrentMethodName = previousMethodName;
        ctx.CurrentMethodIsOverride = previousMethodIsOverride;
        ctx.CurrentMethodOwnerType = previousMethodOwnerType;
        ctx.CurrentNativeEventName = previousNativeEventName;
    }

    // ── Dispatch condition builders ───────────────────────────────────────────

    /// <summary>
    ///     Builds the GML if-condition to select <paramref name="overload"/> over the others
    ///     (method dispatch).
    /// </summary>
    private static string BuildMethodDispatchCondition(
        TgmlMethodDecl overload, List<TgmlMethodDecl> all)
    {
        var count = overload.Params.Count;
        var sameCounts = all.Where(o => o != overload && o.Params.Count == count).ToList();
        var conditions = new List<string> { $"argument_count == {count}" };

        if (sameCounts.Count > 0)
            AppendTypeDistinguisher(conditions, overload.Params, sameCounts.Select(o => o.Params).ToList(), argPrefix: "argument");

        return string.Join(" && ", conditions);
    }

    /// <summary>
    ///     Builds the GML if-condition to select <paramref name="overload"/> over the others
    ///     (constructor dispatch, accounting for type-arg prefix offset).
    /// </summary>
    private static string BuildCtorDispatchCondition(
        TgmlConstructorDecl overload, List<TgmlConstructorDecl> all, int typeArgOffset)
    {
        var count = overload.Params.Count;
        var sameCounts = all.Where(o => o != overload && o.Params.Count == count).ToList();
        var conditions = new List<string> { $"argument_count == {count + typeArgOffset}" };

        if (sameCounts.Count > 0)
            AppendTypeDistinguisher(conditions,
                overload.Params,
                sameCounts.Select(o => o.Params).ToList(),
                argPrefix: "argument",
                offset: typeArgOffset);

        return string.Join(" && ", conditions);
    }

    /// <summary>
    ///     Appends a GML type-check condition at the first parameter position where
    ///     <paramref name="myParams"/> differs from all <paramref name="otherParams"/> lists.
    /// </summary>
    private static void AppendTypeDistinguisher(
        List<string> conditions,
        IReadOnlyList<TgmlParam> myParams,
        List<List<TgmlParam>> otherParams,
        string argPrefix,
        int offset = 0)
    {
        for (var i = 0; i < myParams.Count; i++)
        {
            var myType = myParams[i].Type.Name.Full;
            var conflictAtPos = otherParams.Any(o => o[i].Type.Name.Full == myType);
            if (conflictAtPos) continue;

            var check = GmlTypeCheck(myType, $"{argPrefix}[{i + offset}]");
            if (check is not null) conditions.Add(check);
            break;
        }
    }

    /// <summary>
    ///     Overload specificity score for a method: sum of per-param scores.
    ///     Higher = should be dispatched first (before less-specific fallback).
    ///     number score 0 (GML can't distinguish numeric shapes); string/bool/array/struct score higher.
    /// </summary>
    private static int MethodSpecificity(TgmlMethodDecl m) =>
        m.Params.Sum(p => ParamTypeSpecificity(p.Type.Name.Full));

    /// <summary>Same as <see cref="MethodSpecificity"/> for constructors.</summary>
    private static int CtorSpecificity(TgmlConstructorDecl c) =>
        c.Params.Sum(p => ParamTypeSpecificity(p.Type.Name.Full));

    private static int ParamTypeSpecificity(string tgmlType) =>
        tgmlType switch
        {
            "string" or "bool" or "array" => 3,
            "number" or "int" or "real" => 0,
            _ => 2 // user-defined class / struct
        };

    /// <summary>
    ///     Returns a GML runtime type-test expression for <paramref name="tgmlType"/>,
    ///     or <c>null</c> when no reliable check exists (e.g., numeric overloads).
    /// </summary>
    private static string? GmlTypeCheck(string tgmlType, string argExpr) =>
        tgmlType switch
        {
            "string" => $"is_string({argExpr})",
            "bool"   => $"is_bool({argExpr})",
            "array"  => $"is_array({argExpr})",
            "number" or "int" or "real" => null, // indistinguishable at GML runtime
            _ => $"is_struct({argExpr})"   // user-defined class/struct
        };

    private static void EmitProperty(TgmlPropertyDecl prop, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        if (prop.IsIndexer)
        {
            EmitIndexer(prop, ctx, stmtEmit, w, isStatic: true);
            return;
        }

        var nativePropertyName = ctx.GetNativePropertyName(prop);
        ctx.CurrentPropertyName = prop.Name;

        if (prop.Getter is { } getter)
        {
            ctx.InsideGetter = true;
            ctx.InsideSetter = false;

            if (nativePropertyName is not null && getter.IsAuto)
                w.WriteLine($"static get_{prop.Name} = function() {{ return {nativePropertyName}; }};");
            else if (getter.IsAuto)
                w.WriteLine($"static get_{prop.Name} = function() {{ return __backing_{prop.Name}; }};");
            else
            {
                w.WriteLine($"static get_{prop.Name} = function()");
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
                w.WriteLine($"static set_{prop.Name} = function(value) {{ {nativePropertyName} = value; }};");
            else if (setter.IsAuto)
                w.WriteLine($"static set_{prop.Name} = function(value) {{ __backing_{prop.Name} = value; }};");
            else
            {
                w.WriteLine($"static set_{prop.Name} = function(value)");
                w.OpenBrace();
                if (setter.Body is not null) stmtEmit.EmitBlock(setter.Body, w);
                w.CloseBrace();
            }
        }

        ctx.InsideGetter = false;
        ctx.InsideSetter = false;
        ctx.CurrentPropertyName = null;
    }

    private static void EmitIndexer(TgmlPropertyDecl prop, GenerationContext ctx, StatementEmitter stmtEmit, GmlWriter w, bool isStatic)
    {
        var indexParamName = prop.IndexParam?.Name ?? "index";
        var prefix = isStatic ? "static " : string.Empty;

        if (prop.Getter is { } getter)
        {
            ctx.InsideGetter = true;
            ctx.InsideSetter = false;
            ctx.CurrentPropertyName = null;

            w.WriteLine($"{prefix}get_Item = function({indexParamName})");
            w.OpenBrace();
            if (getter.Body is not null)
                stmtEmit.EmitBlock(getter.Body, w);
            w.CloseBrace();
        }

        if (prop.Setter is { } setter)
        {
            ctx.InsideGetter = false;
            ctx.InsideSetter = true;
            ctx.CurrentPropertyName = null;

            w.WriteLine($"{prefix}set_Item = function({indexParamName}, value)");
            w.OpenBrace();
            if (setter.Body is not null)
                stmtEmit.EmitBlock(setter.Body, w);
            w.CloseBrace();
        }

        ctx.InsideGetter = false;
        ctx.InsideSetter = false;
        ctx.CurrentPropertyName = null;
    }
}

