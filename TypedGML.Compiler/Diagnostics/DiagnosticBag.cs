using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TypedGML.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _all = [];

    public void Report(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        string message,
        SourceLocation location)
    {
        _all.Add(new Diagnostic(code, severity, message, location));
    }

    public bool HasErrors => _all.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    public IReadOnlyList<Diagnostic> All => _all.AsReadOnly();

    public IReadOnlyList<Diagnostic> Errors =>
        new ReadOnlyCollection<Diagnostic>(
            _all.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList());

    public IReadOnlyList<Diagnostic> Warnings =>
        new ReadOnlyCollection<Diagnostic>(
            _all.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ToList());
}
