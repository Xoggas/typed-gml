using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits methods as GML <c>static Name = function(…) { … };</c> assignments,
///     including runtime overload-dispatch wrappers when multiple overloads share a name.
/// </summary>
/// <remarks>
///     Methods that are compiler-synthesised (currently only <c>GetType</c>) are
///     filtered out and must not be passed to this emitter.
///     Operator overloads and abstract methods are silently skipped.
/// </remarks>
internal static class StaticMethodsEmitter
{
    /// <summary>
    ///     Groups <paramref name="methods"/> by name and emits each group.
    ///     <list type="bullet">
    ///         <item>Single overload → <c>static Name = function(…) { … };</c></item>
    ///         <item>
    ///             Multiple overloads → numbered variants <c>Name_0</c> … <c>Name_N</c>
    ///             plus a dispatcher <c>Name</c> that checks <c>argument_count</c>.
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="methods">
    ///     Methods to emit; abstract and operator methods are filtered internally.
    /// </param>
    /// <param name="ctx">Generation context (provides statement emitter access).</param>
    /// <param name="w">Target GML writer.</param>
    public static void Emit(
        IReadOnlyList<TgmlMethodDecl> methods,
        GenerationContext ctx,
        GmlWriter w)
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
                EmitOne(overloads[0].Name, overloads[0], ctx, w);
                continue;
            }

            EmitNumberedOverloads(group.Key, overloads, ctx, w);
            EmitDispatcher(group.Key, overloads, w);
        }
    }

    // ── Single method ─────────────────────────────────────────────────────────

    /// <summary>
    ///     Emits a single <c>static gmlName = function(…) { … };</c>,
    ///     honouring <c>NativeCallName</c> metadata for BCL native-call stubs.
    /// </summary>
    public static void EmitOne(
        string gmlName,
        TgmlMethodDecl method,
        GenerationContext ctx,
        GmlWriter w)
    {
        var paramStr = string.Join(", ", method.Params.Select(p => p.Name));

        if (method.Metadata.TryGetValue("NativeCallName", out var nco) && nco is string nativeCallName)
        {
            var prefix = method.ReturnType.Name.Full == "void" ? string.Empty : "return ";
            w.WriteLine($"static {gmlName} = function({paramStr}) {{ {prefix}{nativeCallName}({paramStr}); }}");
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

    // ── Overload helpers ──────────────────────────────────────────────────────

    private static void EmitNumberedOverloads(
        string baseName,
        IReadOnlyList<TgmlMethodDecl> overloads,
        GenerationContext ctx,
        GmlWriter w)
    {
        for (var i = 0; i < overloads.Count; i++)
        {
            EmitOne($"{baseName}_{i}", overloads[i], ctx, w);
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
        w.WriteLine($"static {baseName} = function()");
        w.OpenBrace();

        for (var di = 0; di < dispatchOrder.Count; di++)
        {
            var (ov, origIdx) = dispatchOrder[di];
            var isLast   = di == dispatchOrder.Count - 1;
            var argExprs = string.Join(", ",
                Enumerable.Range(0, ov.Params.Count).Select(j => $"argument[{j}]"));
            var ret = ov.ReturnType.Name.Full != "void" ? "return " : "";

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
}

