using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits one or more <see cref="GeneratedFile"/>s for a given type declaration.
/// </summary>
/// <remarks>
///     Implementations are registered with <see cref="Generator"/> and selected via
///     <see cref="CanEmit"/>.  Each emitter should handle exactly one category of type
///     (e.g. script classes, GameMaker objects, enums).
/// </remarks>
public interface ICodeEmitter
{
    /// <summary>
    ///     Returns <c>true</c> when this emitter knows how to handle <paramref name="decl"/>.
    /// </summary>
    bool CanEmit(TgmlTypeDecl decl);

    /// <summary>
    ///     Generates GML output files for <paramref name="decl"/>.
    /// </summary>
    /// <param name="decl">The type declaration to emit.</param>
    /// <param name="ctx">Shared mutable context for the current generation pass.</param>
    /// <returns>One or more <see cref="GeneratedFile"/> instances.</returns>
    IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx);
}