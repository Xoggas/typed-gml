namespace TypedGML.Transpiler;

/// <summary>A single .tgml source file to be transpiled.</summary>
public sealed record TgmlSourceFile(string FileName, string Content);