namespace TypedGML.Compiler.Diagnostics;

public sealed record SourceLocation(string FilePath, int Line, int Column);
