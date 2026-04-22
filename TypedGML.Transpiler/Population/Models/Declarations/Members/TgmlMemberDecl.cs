namespace TypedGML.Transpiler.Population.Models;

/// <summary>Abstract base for all member declarations (fields, properties, methods, constructors).</summary>
public abstract class TgmlMemberDecl
{
    public required string Name { get; init; }
    public required AccessModifier Access { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public List<TgmlDecorator> Decorators { get; init; } = new();

    /// <summary>Arbitrary metadata populated by decorator handlers and the checking pipeline.</summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public bool HasDecorator(string name)
    {
        return Decorators.Any(d => d.SimpleName == name || d.Name.Full == name);
    }

    public TgmlDecorator? GetDecorator(string name)
    {
        return Decorators.FirstOrDefault(d => d.SimpleName == name || d.Name.Full == name);
    }
}