namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlDelegateDecl : TgmlTypeDecl
{
    public required TgmlTypeRef ReturnType { get; init; }
    public List<TgmlParam> Params { get; init; } = new();
}
