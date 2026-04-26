using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class BinaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is BinaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (BinaryExpressionNode)node;
        ctx.Writer.Write(
            $"({ctx.Emitter.Render(expression.Left, ctx)} {ExpressionFormatHelper.BinaryOperator(expression.Op)} {ctx.Emitter.Render(expression.Right, ctx)})");
    }
}
