using TypedGML.Compiler.Symbols;

namespace TypedGML.CLI;

internal static class ObjectResourceNameCollector
{
    public static IReadOnlyList<string> Collect(SymbolTable symbols) =>
        symbols.AllTypes
            .Select(type => type.ObjectAssetName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
