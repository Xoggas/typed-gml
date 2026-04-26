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
        if (expression.Body is BlockStatementNode)
        {
            ctx.Emitter.Emit(expression.Body, ctx);
            return;
        }

        ctx.Writer.BeginBlock();
        ctx.Writer.WriteLine($"return {ctx.Emitter.Render(expression.Body, ctx)};");
        ctx.Writer.EndBlock();
    }
}
