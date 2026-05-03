using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class IndexerAccessExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is IndexerAccessExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (IndexerAccessExpressionNode)node;
        if (IndexerAccessRenderer.TryRenderRead(expression, ctx, out var rendered))
        {
            ctx.Writer.Write(rendered);
            return;
        }

        ctx.Writer.Write($"{ctx.Emitter.Render(expression.Target, ctx)}[{ctx.Emitter.Render(expression.Index, ctx)}]");
    }
}
