namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Pure static helpers for primitive-type compatibility.
///     Used by both <see cref="ExprChecker" /> and any other check that needs type arithmetic.
/// </summary>
public static class TypeCompatibility
{
    public static bool IsNumeric(string? t) => BuiltinTypeFacts.IsNumeric(t);
    public static bool IsPrimitive(string? t) => BuiltinTypeFacts.IsPrimitive(t);

    /// <summary>
    ///     True when a value of type <paramref name="rhs" /> can be assigned to a variable
    ///     of type <paramref name="lhs" /> without an explicit cast.
    /// </summary>
    public static bool AreAssignable(string lhs, string rhs)
    {
        if (lhs == rhs) return true;
        if (rhs == "null") return true;              // null → anything
        if (IsNumeric(lhs) && IsNumeric(rhs)) return true; // numeric family

        // Primitive ↔ BCL wrapper equivalence
        if (ArePrimitiveEquivalent(lhs, rhs)) return true;

        return false;
    }

    /// <summary>True when two types can be compared with <c>==</c> / <c>!=</c>.</summary>
    public static bool AreComparable(string a, string b)
    {
        if (a == b) return true;
        if (IsNumeric(a) && IsNumeric(b)) return true;
        if (ArePrimitiveEquivalent(a, b)) return true;
        return false;
    }

    /// <summary>
    ///     True when one type is a GML primitive and the other is its BCL wrapper class,
    ///     e.g. <c>string</c> ↔ <c>System.String</c>.
    /// </summary>
    public static bool ArePrimitiveEquivalent(string a, string b)
    {
        return (IsStringType(a) && IsStringType(b)) ||
               (IsBoolType(a) && IsBoolType(b)) ||
               (IsNumeric(a) && IsNumericWrapper(b)) ||
               (IsNumericWrapper(a) && IsNumeric(b));
    }

    private static bool IsStringType(string t) =>
        t == "string" || t == "System.String";

    private static bool IsBoolType(string t) =>
        t == "bool" || t == "System.Bool";

    private static bool IsNumericWrapper(string t) =>
        t == "System.Number";
}

