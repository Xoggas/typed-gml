namespace TypedGML.Transpiler;

/// <summary>The result of a transpilation run.</summary>
public sealed class TranspileResult
{
    public IReadOnlyList<GeneratedFile> Files { get; init; } = Array.Empty<GeneratedFile>();
    public IReadOnlyList<TranspileDiagnostic> Diagnostics { get; init; } = Array.Empty<TranspileDiagnostic>();
    public bool Success => Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);

    public static TranspileResult FromError(string message, string? file = null, int line = 0)
    {
        return new TranspileResult
            { Diagnostics = new[] { new TranspileDiagnostic(DiagnosticSeverity.Error, message, file, line) } };
    }
}