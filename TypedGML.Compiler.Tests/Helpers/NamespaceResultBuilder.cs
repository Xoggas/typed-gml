using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Tests.Helpers;

internal static class NamespaceResultBuilder
{
    public static IReadOnlyList<string> Build(SymbolTable symbols) =>
        symbols.AllTypes
            .Select(type => NamespaceOf(type.QualifiedName))
            .Where(namespaceName => !string.IsNullOrWhiteSpace(namespaceName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static string NamespaceOf(string qualifiedName)
    {
        var index = qualifiedName.LastIndexOf('.');
        return index < 0 ? string.Empty : qualifiedName[..index];
    }
}
