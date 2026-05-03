namespace TypedGML.Compiler.Diagnostics;

public sealed record Diagnostic(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    string Message,
    SourceLocation Location);
