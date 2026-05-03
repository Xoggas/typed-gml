using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Emission;

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

    public static string Unary(UnaryExpressionNode expression, EmitContext ctx)
    {
        var op = UnaryOperator(expression.Op);
        var operand = ctx.Emitter.Render(expression.Operand, ctx);
        return expression.Operand is LiteralExpressionNode or IdentifierExpressionNode or MemberAccessExpressionNode or InvocationExpressionNode
            ? $"{op}{operand}"
            : $"({op}{operand})";
    }

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
