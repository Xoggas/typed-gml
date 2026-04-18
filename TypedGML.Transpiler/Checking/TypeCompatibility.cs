namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Pure static helpers for primitive-type compatibility.
///     Used by both <see cref="ExprChecker" /> and any other check that needs type arithmetic.
/// </summary>
public static class TypeCompatibility
{
    private static readonly HashSet<string> NumericTypes = ["int", "real"];
    private static readonly HashSet<string> PrimitiveTypes = ["int", "real", "bool", "string"];

    public static bool IsNumeric(string? t) => t is not null && NumericTypes.Contains(t);
    public static bool IsPrimitive(string? t) => t is not null && PrimitiveTypes.Contains(t);

    /// <summary>
    ///     True when a value of type <paramref name="rhs" /> can be assigned to a variable
    ///     of type <paramref name="lhs" /> without an explicit cast.
    /// </summary>
    public static bool AreAssignable(string lhs, string rhs)
    {
        if (lhs == rhs) return true;
        if (rhs == "null") return true;              // null → anything
        if (IsNumeric(lhs) && IsNumeric(rhs)) return true; // int ↔ real
        return false;
    }

    /// <summary>True when two types can be compared with <c>==</c> / <c>!=</c>.</summary>
    public static bool AreComparable(string a, string b)
    {
        if (a == b) return true;
        if (IsNumeric(a) && IsNumeric(b)) return true;
        return false;
    }
}

