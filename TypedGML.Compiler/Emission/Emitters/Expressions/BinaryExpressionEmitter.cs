using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class BinaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is BinaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (BinaryExpressionNode)node;
        if (IsStringConcat(expression, ctx))
        {
            var depth = CountStringConcatNodes(expression, ctx);
            ctx.Writer.Write($"{new string('(', depth)}{RenderStringConcat(expression, ctx)}{new string(')', depth)}");
            return;
        }

        var left = ctx.Emitter.Render(expression.Left, ctx);
        var right = expression.Op is "and" or "or"
            ? ctx.RenderWithoutTempPrelude(expression.Right)
            : ctx.Emitter.Render(expression.Right, ctx);
        ctx.Writer.Write(
            $"({left} {ExpressionFormatHelper.BinaryOperator(expression.Op)} {right})");
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
        if (node is BinaryExpressionNode binary && IsStringConcat(binary, ctx))
            return RenderStringConcat(binary, ctx);

        var rendered = ctx.Emitter.Render(node, ctx);
        return ExpressionTypeLookup.Resolve(node, ctx) is "number" or "bool" ? $"string({rendered})" : rendered;
    }

    private static int CountStringConcatNodes(IAstNode node, EmitContext ctx)
    {
        if (node is not BinaryExpressionNode binary || !IsStringConcat(binary, ctx))
            return 0;

        return 1 + CountStringConcatNodes(binary.Left, ctx) + CountStringConcatNodes(binary.Right, ctx);
    }
}
