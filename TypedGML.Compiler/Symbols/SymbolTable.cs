using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Symbols;

public sealed class SymbolTable(DiagnosticBag diagnostics)
{
    private readonly Dictionary<string, TypeSymbol> _types = new(StringComparer.Ordinal);

    public void Register(string qualifiedName, TypeSymbol symbol)
    {
        var key = MakeKey(qualifiedName, symbol.GenericParameters.Count);
        if (_types.ContainsKey(key))
        {
            diagnostics.Report(
                DiagnosticCode.DuplicateTypeName,
                DiagnosticSeverity.Error,
                $"Duplicate type name '{DisplayName(qualifiedName, symbol.GenericParameters.Count)}'.",
                new SourceLocation(string.Empty, 0, 0));
            return;
        }

        _types[key] = symbol;
    }

    public bool TryResolve(
        string name,
        string? currentNs,
        IReadOnlyList<string> usingPrefixes,
        out TypeSymbol symbol)
    {
        var (baseName, arity) = SplitTypeRef(name);
        return TryResolve(baseName, arity, currentNs, usingPrefixes, out symbol);
    }

    public bool TryResolve(
        string name,
        int arity,
        string? currentNs,
        IReadOnlyList<string> usingPrefixes,
        out TypeSymbol symbol)
    {
        if (name.Contains('.', StringComparison.Ordinal))
            return _types.TryGetValue(MakeKey(name, arity), out symbol!);

        if (!string.IsNullOrEmpty(currentNs) && _types.TryGetValue(MakeKey($"{currentNs}.{name}", arity), out symbol!))
            return true;

        foreach (var prefix in usingPrefixes)
            if (_types.TryGetValue(MakeKey($"{prefix}.{name}", arity), out symbol!))
                return true;

        if (_types.TryGetValue(MakeKey(name, arity), out symbol!))
            return true;

        symbol = null!;
        return false;
    }

    public IReadOnlyList<TypeSymbol> AllTypes => _types.Values.ToList();

    private static string MakeKey(string qualifiedName, int arity) =>
        arity == 0 ? qualifiedName : $"{qualifiedName}`{arity}";

    private static string DisplayName(string qualifiedName, int arity) =>
        arity == 0 ? qualifiedName : $"{qualifiedName}`{arity}";

    private static (string BaseName, int Arity) SplitTypeRef(string typeRef)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return (string.Empty, 0);

        var genericStart = typeRef.IndexOf('<');
        if (genericStart >= 0)
            return (typeRef[..genericStart], CountTypeArguments(typeRef, genericStart));

        var stop = typeRef.IndexOfAny(['?', '[']);
        return (stop >= 0 ? typeRef[..stop] : typeRef, 0);
    }

    private static int CountTypeArguments(string typeRef, int genericStart)
    {
        var depth = 0;
        var count = 1;
        for (var i = genericStart + 1; i < typeRef.Length; i++)
        {
            if (typeRef[i] == '<')
                depth++;
            else if (typeRef[i] == '>')
            {
                if (depth == 0)
                    return count;
                depth--;
            }
            else if (typeRef[i] == ',' && depth == 0)
                count++;
        }

        return 0;
    }
}
