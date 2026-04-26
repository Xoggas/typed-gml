using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ExpressionFormatHelper
{
    public static string BinaryOperator(string op) => op switch
    {
        "and" => "&&",
        "or" => "||",
        _ => op
    };

    public static string UnaryOperator(string op) => op switch
    {
        "not" => "!",
        _ => op
    };

    public static string Literal(LiteralExpressionNode literal) => literal.Kind switch
    {
        LiteralKind.String => $"\"{Escape(literal.Value?.ToString() ?? string.Empty)}\"",
        LiteralKind.Bool => string.Equals(literal.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
        LiteralKind.Null => "undefined",
        _ => literal.Value?.ToString() ?? "undefined"
    };

    public static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
