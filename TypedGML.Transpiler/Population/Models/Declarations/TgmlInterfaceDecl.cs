namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlInterfaceDecl : TgmlTypeDecl
{
    public List<TgmlTypeRef> BaseInterfaces { get; init; } = new();
    public List<TgmlInterfaceMethodDecl> Methods { get; init; } = new();
    public List<TgmlInterfacePropertyDecl> Properties { get; init; } = new();
}