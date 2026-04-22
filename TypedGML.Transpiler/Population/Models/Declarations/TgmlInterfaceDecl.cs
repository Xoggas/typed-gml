namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     An interface declaration.  Interfaces define contracts checked at compile time;
///     they produce no GML output directly but populate the <c>__types</c> struct of
///     every implementing class/struct with the interface's type ID.
/// </summary>
public sealed class TgmlInterfaceDecl : TgmlTypeDecl
{
    /// <summary>Parent interfaces this interface extends, in declaration order.</summary>
    public List<TgmlTypeRef> BaseInterfaces { get; init; } = new();

    /// <summary>Method stubs declared in the interface body.</summary>
    public List<TgmlInterfaceMethodDecl> Methods { get; init; } = new();

    /// <summary>Property stubs declared in the interface body.</summary>
    public List<TgmlInterfacePropertyDecl> Properties { get; init; } = new();
}