namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlUsing
{
    public required TgmlQualifiedName Name { get; init; }
}

public sealed class TgmlNamespace
{
    public required TgmlQualifiedName Name { get; init; }
}

/// <summary>Represents one parsed .tgml source file.</summary>
public sealed class TgmlFile
{
    public string FileName { get; init; } = string.Empty;
    public List<TgmlUsing> Usings { get; init; } = new();
    public List<TgmlNamespace> Namespaces { get; init; } = new();
    public List<TgmlTypeDecl> TypeDecls { get; init; } = new();

    /// <summary>The primary namespace of this file (first declared), or empty string.</summary>
    public string PrimaryNamespace =>
        Namespaces.Count > 0 ? Namespaces[0].Name.Full : string.Empty;

    /// <summary>GML-safe namespace prefix, e.g. "Game_Systems_" or "" if no namespace.</summary>
    public string GmlNamespacePrefix =>
        Namespaces.Count > 0 ? Namespaces[0].Name.Gml + "_" : string.Empty;
}