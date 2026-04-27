namespace TypedGML.Compiler.Tests.Infrastructure;

public sealed record CompilationResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    IReadOnlyDictionary<string, string> Files)
{
    public bool Succeeded => ExitCode == 0;

    public string AllOutput => string.Join(Environment.NewLine, Files.Values);

    public bool HasDiagnostic(string code) =>
        StandardError.Contains(code, StringComparison.Ordinal);

    public string GetRequiredFile(string relativePath)
    {
        var key = Normalize(relativePath);
        return Files.TryGetValue(key, out var content)
            ? content
            : throw new InvalidOperationException($"Missing generated file '{key}'.");
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/');
}
