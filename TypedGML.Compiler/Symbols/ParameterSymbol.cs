namespace TypedGML.Compiler.Symbols;

public sealed record ParameterSymbol(string Name, string TypeRef, bool HasDefault, object? DefaultValue);
