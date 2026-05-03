using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class DefaultExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is DefaultExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (DefaultExpressionNode)node;
        ctx.Writer.Write(DefaultValueRenderer.Render(expression, ctx));
    }
}
