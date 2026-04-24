namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Centralized facts about built-in TypedGML types.
///     Legacy numeric aliases are kept here so semantic checks can avoid cascaded
///     diagnostics while the dedicated type-reference validation reports them.
/// </summary>
public static class BuiltinTypeFacts
{
    private static readonly Dictionary<string, string> PrimitiveAliases = new(StringComparer.Ordinal)
    {
        ["number"] = "number",
        ["Number"] = "number",
        ["System.Number"] = "number",
        ["int"] = "number",
        ["real"] = "number",
        ["long"] = "number",
        ["bool"] = "bool",
        ["Bool"] = "bool",
        ["System.Bool"] = "bool",
        ["string"] = "string",
        ["String"] = "string",
        ["System.String"] = "string"
    };

    private static readonly Dictionary<string, string> PrimitiveBclTypes = new(StringComparer.Ordinal)
    {
        ["number"] = "System.Number",
        ["bool"] = "System.Bool",
        ["string"] = "System.String"
    };

    private static readonly HashSet<string> BuiltInTypes =
    [
        "void",
        "object",
        "any",
        "array",
        "struct",
        "undefined"
    ];

    public static bool IsBuiltIn(string? name)
        => name is not null && (BuiltInTypes.Contains(name) || PrimitiveAliases.ContainsKey(name));

    public static bool IsLegacyNumericAlias(string? name)
        => name is "int" or "real" or "long";

    public static bool IsNumeric(string? name)
        => CanonicalPrimitiveName(name) == "number";

    public static bool IsPrimitive(string? name)
        => CanonicalPrimitiveName(name) is not null;

    public static string? CanonicalPrimitiveName(string? name)
        => name is not null && PrimitiveAliases.TryGetValue(name, out var canonical) ? canonical : null;

    public static string Canonicalize(string name)
        => CanonicalPrimitiveName(name) ?? name;

    public static bool TryGetBclTypeName(string? name, out string bclTypeName)
    {
        bclTypeName = string.Empty;
        var canonical = CanonicalPrimitiveName(name);
        if (canonical is null)
            return false;

        bclTypeName = PrimitiveBclTypes[canonical];
        return true;
    }
}
