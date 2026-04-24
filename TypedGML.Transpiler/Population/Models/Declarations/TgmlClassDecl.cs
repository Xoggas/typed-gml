namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A class declaration.  May represent a plain script class, an abstract class,
///     or a GameMaker object class (when decorated with <c>@Object</c>).
/// </summary>
public sealed class TgmlClassDecl : TgmlTypeDecl
{
    /// <summary>Class modifier: <c>abstract</c>, <c>sealed</c>, <c>static</c>, or none.</summary>
    public ClassModifier ClassModifier { get; init; }

    /// <summary>Base types and implemented interfaces, in declaration order.</summary>
    public List<TgmlTypeRef> BaseTypes { get; init; } = new();

    /// <summary>Field declarations.</summary>
    public List<TgmlFieldDecl> Fields { get; init; } = new();

    /// <summary>Property declarations.</summary>
    public List<TgmlPropertyDecl> Properties { get; init; } = new();

    /// <summary>Method declarations (including operator overloads and conversion operators).</summary>
    public List<TgmlMethodDecl> Methods { get; init; } = new();

    /// <summary>Constructor declarations; may be empty (default constructor is implied).</summary>
    public List<TgmlConstructorDecl> Constructors { get; init; } = new();

    /// <summary>Convenience accessor for the first constructor, or <c>null</c> if none.</summary>
    public TgmlConstructorDecl? Constructor => Constructors.FirstOrDefault();

    /// <summary>Nested type declarations.</summary>
    public List<TgmlTypeDecl> NestedTypes { get; init; } = new();

    /// <summary><c>true</c> when the <c>abstract</c> modifier is present.</summary>
    public bool IsAbstract => ClassModifier == ClassModifier.Abstract;

    /// <summary><c>true</c> when the <c>sealed</c> modifier is present.</summary>
    public bool IsSealed => ClassModifier == ClassModifier.Sealed;

    /// <summary><c>true</c> when the <c>static</c> modifier is present.</summary>
    public bool IsStatic => ClassModifier == ClassModifier.Static;

    /// <summary><c>true</c> when this class has an <c>@Object("…")</c> decorator and compiles as a GameMaker object.</summary>
    public bool IsGameObject => HasDecorator("Object");

    /// <summary>The GML object name from <c>@Object("obj_Name")</c>, or <c>null</c> when not a GameObject.</summary>
    public string? GmlObjectName => GetDecorator("Object")?.GetFirstStringArg();
}
