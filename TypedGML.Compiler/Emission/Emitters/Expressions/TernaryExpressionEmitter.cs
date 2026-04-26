using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class TernaryExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is TernaryExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (TernaryExpressionNode)node;
        ctx.Writer.Write(
            $"({ctx.Emitter.Render(expression.Condition, ctx)} ? {ctx.Emitter.Render(expression.ThenExpr, ctx)} : {ctx.Emitter.Render(expression.ElseExpr, ctx)})");
    }
}
