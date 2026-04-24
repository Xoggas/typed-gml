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
        lhs = BuiltinTypeFacts.Canonicalize(lhs);
        rhs = BuiltinTypeFacts.Canonicalize(rhs);

        if (lhs == rhs) return true;
        if (rhs == "null") return true;              // null → anything

        if (ArePrimitiveEquivalent(lhs, rhs)) return true;

        return false;
    }

    /// <summary>True when two types can be compared with <c>==</c> / <c>!=</c>.</summary>
    public static bool AreComparable(string a, string b)
    {
        a = BuiltinTypeFacts.Canonicalize(a);
        b = BuiltinTypeFacts.Canonicalize(b);

        if (a == b) return true;
        if (ArePrimitiveEquivalent(a, b)) return true;
        return false;
    }

    /// <summary>
    ///     True when one type is a GML primitive and the other is its BCL wrapper class,
    ///     e.g. <c>string</c> ↔ <c>System.String</c>.
    /// </summary>
    public static bool ArePrimitiveEquivalent(string a, string b)
    {
        return BuiltinTypeFacts.CanonicalPrimitiveName(a) is { } left &&
               BuiltinTypeFacts.CanonicalPrimitiveName(b) is { } right &&
               left == right;
    }
}

