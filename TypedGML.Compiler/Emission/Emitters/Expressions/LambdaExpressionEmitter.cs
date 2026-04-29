using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class LambdaExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is LambdaExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (LambdaExpressionNode)node;
        var parameters = string.Join(", ", expression.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function({parameters})");
        if (expression.Body is not BlockStatementNode)
        {
            ctx.Writer.Write($" {{ return {ctx.Emitter.Render(expression.Body, ctx)}; }}");
            return;
        }

        ctx.Emitter.Emit(expression.Body, ctx);
    }
}
