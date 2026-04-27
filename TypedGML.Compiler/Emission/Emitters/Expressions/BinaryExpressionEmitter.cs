using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class BinaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is BinaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (BinaryExpressionNode)node;
        var left = ctx.Emitter.Render(expression.Left, ctx);
        var right = ctx.Emitter.Render(expression.Right, ctx);
        var leftType = ExpressionTypeLookup.Resolve(expression.Left, ctx);
        var rightType = ExpressionTypeLookup.Resolve(expression.Right, ctx);
        if (expression.Op == "+" && leftType == "string" && rightType == "number")
        {
            ctx.Writer.Write($"({left} + string({right}))");
            return;
        }

        if (expression.Op == "+" && leftType == "number" && rightType == "string")
        {
            ctx.Writer.Write($"(string({left}) + {right})");
            return;
        }

        ctx.Writer.Write(
            $"({left} {ExpressionFormatHelper.BinaryOperator(expression.Op)} {right})");
    }
}
