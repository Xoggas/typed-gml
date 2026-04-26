using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class NullConditionalExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is NullConditionalExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (NullConditionalExpressionNode)node;
        var target = ctx.Emitter.Render(expression.Target, ctx);
        ctx.Writer.Write($"({target} != undefined ? {target}.{expression.MemberName} : undefined)");
    }
}
