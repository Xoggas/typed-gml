using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits file-level GML helper functions for user-defined operator overloads and
///     user-defined conversion operators.
/// </summary>
/// <remarks>
///     In GML, operator helpers must be plain functions (not struct <c>static</c> members)
///     because the GML runtime looks them up by global name.  They must therefore be
///     emitted <b>before</b> the constructor function in the same file.
/// </remarks>
internal static class OperatorHelperEmitter
{
    /// <summary>
    ///     Emits a <c>function HelperName(…) { … }</c> declaration for every
    ///     user-defined operator or conversion operator in <paramref name="methods"/>
    ///     that has a body.  Methods without a body (abstract / interface stubs) are skipped.
    /// </summary>
    /// <param name="owner">
    ///     The type that declares the operators; used to derive the GML helper name via
    ///     <see cref="OperatorFacts.GetHelperName"/>.
    /// </param>
    /// <param name="methods">Full method list of the type; non-operator methods are filtered.</param>
    /// <param name="ctx">Generation context (saved and restored around helper emission).</param>
    /// <param name="w">Target GML writer.</param>
    /// <returns>
    ///     <c>true</c> when at least one helper was emitted, so the caller can add a
    ///     blank separator line before the constructor.
    /// </returns>
    public static bool Emit(
        TgmlTypeDecl owner,
        IReadOnlyList<TgmlMethodDecl> methods,
        GenerationContext ctx,
        GmlWriter w)
    {
        var helpers = methods.Where(m => m.IsUserDefinedOperator && m.Body is not null).ToList();
        if (helpers.Count == 0) return false;

        // Save and restore the mutable context state around helper emission.
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

        return true;
    }
}

