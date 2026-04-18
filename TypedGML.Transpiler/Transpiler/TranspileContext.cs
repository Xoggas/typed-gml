using TypedGML.Transpiler.Checking;

namespace TypedGML.Transpiler;

/// <summary>
///     Shared mutable state passed through the entire population → checking → generation pipeline.
/// </summary>
public sealed class TranspileContext
{
    private readonly List<TranspileDiagnostic> _diagnostics = new();
    public TypeTable TypeTable { get; } = new();
    public SymbolTable SymbolTable { get; } = new();
    public IReadOnlyList<TranspileDiagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void AddError(string message, string? file = null, int line = 0, int column = 0)
    {
        _diagnostics.Add(new TranspileDiagnostic(DiagnosticSeverity.Error, message, file, line, column));
    }

    public void AddWarning(string message, string? file = null, int line = 0, int column = 0)
    {
        _diagnostics.Add(new TranspileDiagnostic(DiagnosticSeverity.Warning, message, file, line, column));
    }

    public void AddInfo(string message, string? file = null, int line = 0)
    {
        _diagnostics.Add(new TranspileDiagnostic(DiagnosticSeverity.Info, message, file, line));
    }
}