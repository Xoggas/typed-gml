using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _all = [];
    private readonly Dictionary<TypeSymbol, HashSet<DiagnosticCode>> _typeErrors = [];

    public void Report(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        string message,
        SourceLocation location)
    {
        _all.Add(new Diagnostic(code, severity, message, location));
    }

    public void Report(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        string message,
        SourceLocation location,
        TypeSymbol type)
    {
        Report(code, severity, message, location);
        if (severity == DiagnosticSeverity.Error)
            AddTypeError(code, type);
    }

    public bool HasError(DiagnosticCode code, TypeSymbol type) =>
        _typeErrors.TryGetValue(type, out var codes) && codes.Contains(code);

    public bool HasErrors => _all.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    public IReadOnlyList<Diagnostic> All => _all.AsReadOnly();

    public IReadOnlyList<Diagnostic> Errors =>
        new ReadOnlyCollection<Diagnostic>(
            _all.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList());

    public IReadOnlyList<Diagnostic> Warnings =>
        new ReadOnlyCollection<Diagnostic>(
            _all.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ToList());

    private void AddTypeError(DiagnosticCode code, TypeSymbol type)
    {
        if (!_typeErrors.TryGetValue(type, out var codes))
        {
            codes = [];
            _typeErrors.Add(type, codes);
        }

        codes.Add(code);
    }
}
