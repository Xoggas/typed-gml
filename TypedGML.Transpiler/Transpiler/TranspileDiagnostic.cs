namespace TypedGML.Transpiler;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}

/// <summary>A single diagnostic message (error/warning) produced during transpilation.</summary>
public sealed record TranspileDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? File = null,
    int Line = 0,
    int Column = 0)
{
    public override string ToString()
    {
        return File is not null
            ? $"[{Severity}] {File}({Line},{Column}): {Message}"
            : $"[{Severity}] {Message}";
    }
}