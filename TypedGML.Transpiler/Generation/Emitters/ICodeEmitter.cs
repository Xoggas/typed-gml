using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>Emits one or more <see cref="GeneratedFile" />s for a type declaration.</summary>
public interface ICodeEmitter
{
    bool CanEmit(TgmlTypeDecl decl);
    IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx);
}