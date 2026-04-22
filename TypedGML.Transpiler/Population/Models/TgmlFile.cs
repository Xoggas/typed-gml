namespace TypedGML.Transpiler.Population.Models;

/// <summary>A <c>using</c> directive that imports a namespace into the current file's scope.</summary>
public sealed class TgmlUsing
{
    /// <summary>The imported namespace name.</summary>
    public required TgmlQualifiedName Name { get; init; }
}

/// <summary>A <c>namespace</c> declaration scoping all types in this file.</summary>
public sealed class TgmlNamespace
{
    /// <summary>The namespace name.</summary>
    public required TgmlQualifiedName Name { get; init; }
}

/// <summary>Represents one parsed <c>.tgml</c> source file and all its top-level declarations.</summary>
public sealed class TgmlFile
{
    /// <summary>The source file path, relative to the project root.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Using directives declared at the top of the file.</summary>
    public List<TgmlUsing> Usings { get; init; } = new();

    /// <summary>Namespace declarations; typically exactly one per file.</summary>
    public List<TgmlNamespace> Namespaces { get; init; } = new();

    /// <summary>All top-level type declarations in this file.</summary>
    public List<TgmlTypeDecl> TypeDecls { get; init; } = new();

    /// <summary>
    ///     The primary namespace of this file (first declared), or an empty string when
    ///     no <c>namespace</c> statement is present.
    /// </summary>
    public string PrimaryNamespace =>
        Namespaces.Count > 0 ? Namespaces[0].Name.Full : string.Empty;

    /// <summary>
    ///     GML-safe namespace prefix with a trailing underscore, e.g. <c>"Game_Systems_"</c>,
    ///     or an empty string when no namespace is declared.
    /// </summary>
    public string GmlNamespacePrefix =>
        Namespaces.Count > 0 ? Namespaces[0].Name.Gml + "_" : string.Empty;
}