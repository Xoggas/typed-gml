using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class ArrayLiteralExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ArrayLiteralExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (ArrayLiteralExpressionNode)node;
        ctx.Writer.Write($"[{string.Join(", ", expression.Elements.Select(e => ctx.Emitter.Render(e, ctx)))}]");
    }
}
