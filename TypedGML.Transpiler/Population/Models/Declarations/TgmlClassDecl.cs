namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlClassDecl : TgmlTypeDecl
{
    public ClassModifier ClassModifier { get; init; }
    public List<TgmlTypeRef> BaseTypes { get; init; } = new();
    public List<TgmlFieldDecl> Fields { get; init; } = new();
    public List<TgmlPropertyDecl> Properties { get; init; } = new();
    public List<TgmlMethodDecl> Methods { get; init; } = new();
    public List<TgmlConstructorDecl> Constructors { get; init; } = new();
    public TgmlConstructorDecl? Constructor => Constructors.FirstOrDefault();
    public List<TgmlTypeDecl> NestedTypes { get; init; } = new();

    public bool IsAbstract => ClassModifier == ClassModifier.Abstract;
    public bool IsSealed => ClassModifier == ClassModifier.Sealed;

    /// <summary>True if this class has the @Object decorator (compiles as a GameMaker object).</summary>
    public bool IsGameObject => HasDecorator("Object");

    /// <summary>The GML object name from @Object("obj_Name"), or null.</summary>
    public string? GmlObjectName =>
        GetDecorator("Object")?.GetFirstStringArg();
}