using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class NameofExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is NameofExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (NameofExpressionNode)node;
        ctx.Writer.Write($"\"{ExpressionFormatHelper.Escape(expression.Chain.LastOrDefault() ?? string.Empty)}\"");
    }
}
