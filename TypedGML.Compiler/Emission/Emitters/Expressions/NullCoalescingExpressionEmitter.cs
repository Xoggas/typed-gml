using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class NullCoalescingExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is NullCoalescingExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (NullCoalescingExpressionNode)node;
        var left = ctx.Emitter.Render(expression.Left, ctx);
        ctx.Writer.Write($"({left} != undefined ? {left} : {ctx.Emitter.Render(expression.Right, ctx)})");
    }
}
