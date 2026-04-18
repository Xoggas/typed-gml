namespace TypedGML.Transpiler.Population.Models;

/// <summary>A dot-separated qualified name such as "Game.Systems.Player".</summary>
public sealed class TgmlQualifiedName
{
    public List<string> Parts { get; init; } = new();

    /// <summary>Dot-joined form: "Game.Systems.Player"</summary>
    public string Full => string.Join(".", Parts);

    /// <summary>Underscore-joined GML identifier: "Game_Systems_Player"</summary>
    public string Gml => string.Join("_", Parts);

    /// <summary>The simple (last) name component.</summary>
    public string Simple => Parts.Count > 0 ? Parts[^1] : string.Empty;

    public override string ToString()
    {
        return Full;
    }
}