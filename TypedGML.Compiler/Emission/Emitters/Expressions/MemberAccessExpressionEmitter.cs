using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class MemberAccessExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is MemberAccessExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (MemberAccessExpressionNode)node;
        if (StaticMemberAccessHelper.TryRenderRead(expression, ctx, out var rendered))
        {
            ctx.Writer.Write(rendered);
            return;
        }

        ctx.Writer.Write($"{ctx.Emitter.Render(expression.Target, ctx)}.{expression.MemberName}");
    }
}
