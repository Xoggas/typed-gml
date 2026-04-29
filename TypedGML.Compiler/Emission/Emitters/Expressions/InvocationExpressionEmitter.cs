using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class InvocationExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is InvocationExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (InvocationExpressionNode)node;
        if (NullConditionalInvocationHelper.TryRender(expression, ctx, out var conditionalInvocation))
        {
            ctx.Writer.Write(conditionalInvocation);
            return;
        }

        if (MethodInvocationHelper.TryRender(expression, ctx, out var methodTarget, out var methodArgs))
        {
            ctx.Writer.Write($"{methodTarget}({methodArgs})");
            return;
        }

        var target = ExpressionCallHelper.RenderTarget(expression.Target, ctx);
        var args = ExpressionCallHelper.JoinArguments(expression.Target, expression.PositionalArgs, expression.NamedArgs, ctx);
        if (ExpressionSymbolHelper.IsDelegateTarget(expression.Target, ctx))
        {
            var suffix = string.IsNullOrEmpty(args) ? string.Empty : $", {args}";
            ctx.Writer.Write($"__tgml_invoke_delegate({target}{suffix})");
            return;
        }

        ctx.Writer.Write($"{target}({args})");
    }
}
