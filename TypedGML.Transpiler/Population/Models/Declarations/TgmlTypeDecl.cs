namespace TypedGML.Transpiler.Population.Models;

/// <summary>Abstract base for all type declarations (class, struct, enum, interface).</summary>
public abstract class TgmlTypeDecl
{
    public required string Name { get; init; }
    public required AccessModifier Access { get; init; }
    public List<TgmlDecorator> Decorators { get; init; } = new();
    public List<TgmlTypeParam> TypeParams { get; init; } = new();

    /// <summary>
    ///     Fully-qualified name computed after population (namespace + name).
    ///     Set by the populator from the file's namespace declarations.
    /// </summary>
    public string? QualifiedName { get; set; }

    /// <summary>Arbitrary metadata populated by decorator handlers and checks.</summary>
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