using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class GenericTypeSubstitution
{
    public static IReadOnlyDictionary<string, string> Map(TypeSymbol type, IReadOnlyList<string> typeArgs) =>
        type.GenericParameters.Zip(typeArgs)
            .ToDictionary(pair => pair.First.Name, pair => pair.Second, StringComparer.Ordinal);

    public static string Substitute(string typeRef, IReadOnlyDictionary<string, string> map)
    {
        if (map.Count == 0 || string.IsNullOrWhiteSpace(typeRef))
            return typeRef;

        var root = TypeReferenceHelper.RootName(typeRef);
        return map.TryGetValue(root, out var replacement)
            ? replacement + typeRef[root.Length..]
            : typeRef;
    }
}
