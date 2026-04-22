namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A struct declaration.  Structs compile to GML constructor functions just like classes
///     but use value-type default initialisers and cannot be used as GameMaker objects.
/// </summary>
public sealed class TgmlStructDecl : TgmlTypeDecl
{
    /// <summary><c>true</c> when the <c>readonly</c> modifier is present.</summary>
    public bool IsReadonly { get; init; }

    /// <summary>Implemented interfaces, in declaration order.</summary>
    public List<TgmlTypeRef> BaseTypes { get; init; } = new();

    /// <summary>Field declarations.</summary>
    public List<TgmlFieldDecl> Fields { get; init; } = new();

    /// <summary>Property declarations.</summary>
    public List<TgmlPropertyDecl> Properties { get; init; } = new();

    /// <summary>Method declarations.</summary>
    public List<TgmlMethodDecl> Methods { get; init; } = new();

    /// <summary>Constructor declarations.</summary>
    public List<TgmlConstructorDecl> Constructors { get; init; } = new();

    /// <summary>Convenience accessor for the first constructor, or <c>null</c> if none.</summary>
    public TgmlConstructorDecl? Constructor => Constructors.FirstOrDefault();

    /// <summary>Nested type declarations.</summary>
    public List<TgmlTypeDecl> NestedTypes { get; init; } = new();
}