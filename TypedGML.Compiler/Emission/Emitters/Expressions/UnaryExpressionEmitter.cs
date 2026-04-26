using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class UnaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is UnaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (UnaryExpressionNode)node;
        ctx.Writer.Write($"({ExpressionFormatHelper.UnaryOperator(expression.Op)}{ctx.Emitter.Render(expression.Operand, ctx)})");
    }
}
