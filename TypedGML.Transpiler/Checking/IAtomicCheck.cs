using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     A single atomic semantic check that operates on all parsed files.
/// </summary>
/// <remarks>
///     Each implementation should focus on exactly one concern (SRP).
///     Checks are grouped into phases by <see cref="CheckPipeline"/>;
///     earlier phases run first and later phases are skipped on error.
///     Implementations must be stateless — a new instance should produce identical
///     results when called multiple times on the same input.
/// </remarks>
public interface IAtomicCheck
{
    /// <summary>
    ///     Executes this check against all <paramref name="files"/>,
    ///     reporting any violations through <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    ///     The shared transpile context; call <c>context.AddError</c> /
    ///     <c>context.AddWarning</c> to report diagnostics.
    /// </param>
    /// <param name="files">All parsed source files, including BCL files.</param>
    void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files);
}