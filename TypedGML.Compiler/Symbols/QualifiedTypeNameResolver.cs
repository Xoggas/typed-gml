namespace TypedGML.Compiler.Symbols;

public static class QualifiedTypeNameResolver
{
    public static bool TryResolve(
        IReadOnlyList<string> parts,
        SymbolTable symbols,
        string? currentNamespace,
        IReadOnlyList<string> usingPrefixes,
        out TypeSymbol type,
        out IReadOnlyList<string> memberParts)
    {
        for (var length = parts.Count; length >= 1; length--)
        {
            var candidate = string.Join(".", parts.Take(length));
            if (!symbols.TryResolve(candidate, currentNamespace, usingPrefixes, out type))
                continue;

            memberParts = parts.Skip(length).ToList();
            return true;
        }

        type = null!;
        memberParts = [];
        return false;
    }

    public static bool IsNamespacePrefix(IReadOnlyList<string> parts, SymbolTable symbols)
    {
        if (parts.Count == 0)
            return false;

        var prefix = string.Join(".", parts);
        return symbols.AllTypes.Any(type =>
            type.QualifiedName.StartsWith($"{prefix}.", StringComparison.Ordinal));
    }
}
