namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlFieldDecl : TgmlMemberDecl
{
    public required TgmlTypeRef Type { get; init; }
    public bool IsStatic { get; init; }
    public bool IsReadonly { get; init; }
    public bool IsConst { get; init; }
    public TgmlExpression? Initializer { get; init; }
}