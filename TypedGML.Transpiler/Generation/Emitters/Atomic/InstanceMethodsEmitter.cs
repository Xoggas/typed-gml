using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits methods as GML <b>instance</b> function assignments:
///     <c>Name = function(…) { … }</c> (no <c>static</c> keyword).
/// </summary>
/// <remarks>
///     Used exclusively inside GameMaker object Create events, where methods are
///     bound to the instance rather than the struct prototype.
///     Abstract methods are silently skipped.
/// </remarks>
internal static class InstanceMethodsEmitter
{
    /// <summary>
    ///     Groups <paramref name="methods"/> by name and emits each group.
    ///     Multiple overloads produce numbered variants plus a runtime-dispatch wrapper.
    /// </summary>
    /// <param name="methods">Methods to emit (abstract methods are filtered internally).</param>
    /// <param name="declaringType">
    ///     The class that declares these methods; stored in <see cref="GenerationContext"/> for
    ///     correct <c>base</c>-call resolution during body emission.
    /// </param>
    /// <param name="ctx">Generation context.</param>
    /// <param name="w">Target GML writer.</param>
    public static void Emit(
        IReadOnlyList<TgmlMethodDecl> methods,
        TgmlClassDecl declaringType,
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
                EmitOne(overloads[0].Name, overloads[0], declaringType, ctx, w);
                continue;
            }

            EmitNumberedOverloads(group.Key, overloads, declaringType, ctx, w);
            EmitDispatcher(group.Key, overloads, w);
        }
    }

    // ── Single method ─────────────────────────────────────────────────────────

    /// <summary>
    ///     Emits a single <c>methodName = function(…) { … }</c>,
    ///     honouring <c>NativeCallName</c> metadata for native-call stubs.
    /// </summary>
    public static void EmitOne(
        string methodName,
        TgmlMethodDecl method,
        TgmlClassDecl declaringType,
        GenerationContext ctx,
        GmlWriter w)
    {
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));

        ctx.CurrentMethodName       = method.Name;
        ctx.CurrentMethodIsOverride = method.IsOverride;
        ctx.CurrentMethodOwnerType  = declaringType;
        ctx.CurrentNativeEventName  = null;

        if (method.Metadata.TryGetValue("NativeCallName", out var nco) && nco is string nativeCallName)
        {
            var prefix = method.ReturnType.Name.Full == "void" ? string.Empty : "return ";
            w.WriteLine($"{methodName} = function({paramStr}) {{ {prefix}{nativeCallName}({paramStr}); }}");
            ClearContext(ctx);
            return;
        }

        var stmtEmit = new StatementEmitter(ctx);
        w.WriteLine($"{methodName} = function({paramStr})");
        w.OpenBrace();
        if (method.Body is not null) stmtEmit.EmitBlock(method.Body, w);
        w.CloseBrace();
        ClearContext(ctx);
    }

    // ── Overload helpers ──────────────────────────────────────────────────────

    private static void EmitNumberedOverloads(
        string baseName,
        IReadOnlyList<TgmlMethodDecl> overloads,
        TgmlClassDecl declaringType,
        GenerationContext ctx,
        GmlWriter w)
    {
        for (var i = 0; i < overloads.Count; i++)
        {
            EmitOne($"{baseName}_{i}", overloads[i], declaringType, ctx, w);
            if (i < overloads.Count - 1) w.WriteLine();
        }
    }

    private static void EmitDispatcher(
        string baseName,
        IReadOnlyList<TgmlMethodDecl> overloads,
        GmlWriter w)
    {
        var dispatchOrder = overloads
            .Select((m, idx) => (Method: m, Idx: idx))
            .OrderByDescending(x => OverloadDispatchHelper.MethodSpecificity(x.Method))
            .ToList();

        w.WriteLine();
        w.WriteLine($"{baseName} = function()");
        w.OpenBrace();

        for (var di = 0; di < dispatchOrder.Count; di++)
        {
            var (ov, origIdx) = dispatchOrder[di];
            var isLast   = di == dispatchOrder.Count - 1;
            var argExprs = string.Join(", ",
                Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
            var ret = ov.ReturnType.Name.Full != "void" ? "return " : string.Empty;

            if (isLast)
                w.WriteLine($"else {{ {ret}{baseName}_{origIdx}({argExprs}); }}");
            else
            {
                var cond    = OverloadDispatchHelper.BuildMethodDispatchCondition(ov, overloads);
                var keyword = di == 0 ? "if" : "else if";
                w.WriteLine($"{keyword} ({cond}) {{ {ret}{baseName}_{origIdx}({argExprs}); }}");
            }
        }

        w.CloseBrace();
    }

    private static void ClearContext(GenerationContext ctx)
    {
        ctx.CurrentMethodName       = null;
        ctx.CurrentMethodIsOverride = false;
        ctx.CurrentMethodOwnerType  = null;
        ctx.CurrentNativeEventName  = null;
    }
}

