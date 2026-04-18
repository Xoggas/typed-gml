namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlEnumMember
{
    public List<TgmlDecorator> Decorators { get; init; } = new();
    public required string Name { get; init; }

    /// <summary>Explicit integer value expression, or null for auto-increment.</summary>
    public TgmlIntLiteralExpr? Value { get; init; }
}

public sealed class TgmlEnumDecl : TgmlTypeDecl
{
    public List<TgmlEnumMember> Members { get; init; } = new();
}