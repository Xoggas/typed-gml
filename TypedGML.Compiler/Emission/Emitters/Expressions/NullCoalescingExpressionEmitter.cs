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
        if (ctx.CanEmitTempPrelude && !IsSimple(expression.Left))
        {
            var temp = ctx.NextTempVarName();
            ctx.AddTempPreludeLine($"var {temp} = {left};");
            left = temp;
        }

        ctx.Writer.Write($"({left} != undefined ? {left} : {ctx.RenderWithoutTempPrelude(expression.Right)})");
    }

    private static bool IsSimple(IAstNode node) =>
        node is IdentifierExpressionNode or LiteralExpressionNode;
}
