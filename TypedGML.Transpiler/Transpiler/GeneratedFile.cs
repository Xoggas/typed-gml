namespace TypedGML.Transpiler;

/// <summary>A single GML file produced by the transpiler.</summary>
/// <param name="Path">Relative path inside the GameMaker project (e.g. "Scripts/Vec2/Vec2.gml").</param>
/// <param name="Content">The GML source content.</param>
public sealed record GeneratedFile(string Path, string Content);