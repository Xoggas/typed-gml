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
            var rendered = ctx.Emitter.Render(expression.Body, ctx);
            ctx.Writer.Write(ShouldReturn(ctx) ? $" {{ return {rendered}; }}" : $" {{ {rendered}; }}");
            return;
        }

        ctx.Emitter.Emit(expression.Body, ctx);
    }

    private static bool ShouldReturn(EmitContext ctx) =>
        EmitDelegateTypeHelper.TrySignature(ctx.CurrentExpectedType, ctx, out var returnType, out _) &&
        returnType != "void";
}
