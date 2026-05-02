using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class BinaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is BinaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (BinaryExpressionNode)node;
        ctx.Writer.Write(Render(expression, ctx, null, false));
    }

    private static string Render(BinaryExpressionNode expression, EmitContext ctx, BinaryExpressionNode? parent, bool isRight)
    {
        var rendered = IsStringConcat(expression, ctx)
            ? RenderStringConcat(expression, ctx)
            : RenderBinary(expression, ctx);

        return NeedsParentheses(expression, parent, isRight) ? $"({rendered})" : rendered;
    }

    private static string RenderBinary(BinaryExpressionNode expression, EmitContext ctx)
    {
        var left = RenderChild(expression.Left, ctx, expression, false);
        var right = RenderChild(expression.Right, ctx, expression, true);
        return $"{left} {ExpressionFormatHelper.BinaryOperator(expression.Op)} {right}";
    }

    private static string RenderChild(IAstNode node, EmitContext ctx, BinaryExpressionNode parent, bool isRight)
    {
        if (node is BinaryExpressionNode binary)
            return Render(binary, ctx, parent, isRight);

        return parent.Op is "and" or "or" && isRight
            ? ctx.RenderWithoutTempPrelude(node)
            : ctx.Emitter.Render(node, ctx);
    }

    private static bool IsStringConcat(BinaryExpressionNode expression, EmitContext ctx)
    {
        if (expression.Op != "+")
            return false;

        return ExpressionTypeLookup.Resolve(expression.Left, ctx) == "string" ||
            ExpressionTypeLookup.Resolve(expression.Right, ctx) == "string";
    }

    private static string RenderStringConcat(BinaryExpressionNode expression, EmitContext ctx) =>
        $"{RenderStringConcatPart(expression.Left, ctx)} + {RenderStringConcatPart(expression.Right, ctx)}";

    private static string RenderStringConcatPart(IAstNode node, EmitContext ctx)
    {
        var rendered = node is BinaryExpressionNode binary
            ? Render(binary, ctx, parent: null, isRight: false)
            : ctx.Emitter.Render(node, ctx);
        return NeedsStringConversion(node, ctx) ? $"string({rendered})" : rendered;
    }

    private static bool NeedsStringConversion(IAstNode node, EmitContext ctx)
    {
        var typeRef = ExpressionTypeLookup.Resolve(node, ctx);
        if (typeRef is "number" or "bool")
            return true;

        return ExpressionSymbolHelper.TryResolveType(ctx, typeRef, out var type) && type.Kind == TypeKind.Enum;
    }

    private static bool NeedsParentheses(BinaryExpressionNode expression, BinaryExpressionNode? parent, bool isRight)
    {
        if (parent is null)
            return false;

        var childPrecedence = Precedence(expression.Op);
        var parentPrecedence = Precedence(parent.Op);
        if (childPrecedence < parentPrecedence)
            return true;

        return isRight && childPrecedence == parentPrecedence && !IsAssociative(parent.Op);
    }

    private static int Precedence(string op) => op switch
    {
        "*" or "/" or "%" => 6,
        "+" or "-" => 5,
        "<" or ">" or "<=" or ">=" => 4,
        "==" or "!=" => 3,
        "and" => 2,
        "or" => 1,
        _ => 0
    };

    private static bool IsAssociative(string op) => op is "+" or "*" or "and" or "or";
}
