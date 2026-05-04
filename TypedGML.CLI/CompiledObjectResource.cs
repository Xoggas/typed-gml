namespace TypedGML.CLI;

internal sealed record CompiledObjectResource(
    string Name,
    IReadOnlyDictionary<string, string> Events,
    string? SourceFilePath);
