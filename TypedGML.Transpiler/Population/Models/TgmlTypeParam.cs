namespace TypedGML.Transpiler.Population.Models;

/// <summary>A generic type parameter declaration, e.g. "T : IFoo".</summary>
public sealed class TgmlTypeParam
{
    public required string Name { get; init; }
    public TgmlTypeRef? Constraint { get; init; }
}