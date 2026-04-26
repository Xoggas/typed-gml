using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Symbols;

public sealed class SymbolTable(DiagnosticBag diagnostics)
{
    private readonly Dictionary<string, TypeSymbol> _types = new(StringComparer.Ordinal);

    public void Register(string qualifiedName, TypeSymbol symbol)
    {
        if (_types.ContainsKey(qualifiedName))
        {
            diagnostics.Report(
                DiagnosticCode.DuplicateTypeName,
                DiagnosticSeverity.Error,
                $"Duplicate type name '{qualifiedName}'.",
                new SourceLocation(string.Empty, 0, 0));
            return;
        }

        _types[qualifiedName] = symbol;
    }

    public bool TryResolve(
        string name,
        string? currentNs,
        IReadOnlyList<string> usingPrefixes,
        out TypeSymbol symbol)
    {
        if (_types.TryGetValue(name, out symbol!))
            return true;

        if (!string.IsNullOrEmpty(currentNs) && _types.TryGetValue($"{currentNs}.{name}", out symbol!))
            return true;

        foreach (var prefix in usingPrefixes)
            if (_types.TryGetValue($"{prefix}.{name}", out symbol!))
                return true;

        symbol = null!;
        return false;
    }

    public IReadOnlyList<TypeSymbol> AllTypes => _types.Values.ToList();
}
