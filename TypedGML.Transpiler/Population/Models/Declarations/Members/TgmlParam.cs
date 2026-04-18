namespace TypedGML.Transpiler.Population.Models;

/// <summary>A method/constructor/lambda parameter.</summary>
public sealed class TgmlParam
{
    public List<TgmlDecorator> Decorators { get; init; } = new();
    public required TgmlTypeRef Type { get; init; }
    public required string Name { get; init; }
    public TgmlExpression? Default { get; init; }
}