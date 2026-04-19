using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Helpers for recognizing compile-time constant expressions supported by the transpiler.
/// </summary>
public static class ConstantExpressionFacts
{
    /// <summary>
    ///     Returns <c>true</c> when <paramref name="expr"/> is a supported compile-time constant:
    ///     a literal, <c>null</c>, a parenthesized constant, or a unary operation applied to a constant.
    /// </summary>
    public static bool IsCompileTimeConstant(TgmlExpression expr) =>
        expr switch
        {
            TgmlBoolLiteralExpr => true,
            TgmlIntLiteralExpr => true,
            TgmlRealLiteralExpr => true,
            TgmlStringLiteralExpr => true,
            TgmlNullExpr => true,
            TgmlDefaultExpr => true,
            TgmlParenExpr p => IsCompileTimeConstant(p.Inner),
            TgmlUnaryExpr u => u.Operator is "-" or "~" or "not" && IsCompileTimeConstant(u.Operand),
            _ => false
        };
}
