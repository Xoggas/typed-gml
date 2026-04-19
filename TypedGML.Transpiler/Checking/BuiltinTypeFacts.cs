namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Centralized facts about built-in TypedGML types.
///     Legacy numeric aliases are kept here so semantic checks can avoid cascaded
///     diagnostics while the dedicated type-reference validation reports them.
/// </summary>
public static class BuiltinTypeFacts
{
    private static readonly HashSet<string> BuiltInTypes =
    [
        "number",
        "string",
        "bool",
        "void",
        "object",
        "any",
        "array",
        "struct",
        "undefined"
    ];

    private static readonly HashSet<string> LegacyNumericAliases =
    [
        "int",
        "real"
    ];

    private static readonly HashSet<string> NumericTypes =
    [
        "number",
        "int",
        "real"
    ];

    private static readonly HashSet<string> PrimitiveTypes =
    [
        "number",
        "int",
        "real",
        "bool",
        "string"
    ];

    public static bool IsBuiltIn(string? name)
        => name is not null && (BuiltInTypes.Contains(name) || LegacyNumericAliases.Contains(name));

    public static bool IsLegacyNumericAlias(string? name)
        => name is not null && LegacyNumericAliases.Contains(name);

    public static bool IsNumeric(string? name)
        => name is not null && NumericTypes.Contains(name);

    public static bool IsPrimitive(string? name)
        => name is not null && PrimitiveTypes.Contains(name);
}
