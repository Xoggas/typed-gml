namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlStructDecl : TgmlTypeDecl
{
    public bool IsReadonly { get; init; }
    public List<TgmlTypeRef> BaseTypes { get; init; } = new();
    public List<TgmlFieldDecl> Fields { get; init; } = new();
    public List<TgmlPropertyDecl> Properties { get; init; } = new();
    public List<TgmlMethodDecl> Methods { get; init; } = new();
    public List<TgmlConstructorDecl> Constructors { get; init; } = new();
    public TgmlConstructorDecl? Constructor => Constructors.FirstOrDefault();
    public List<TgmlTypeDecl> NestedTypes { get; init; } = new();
}