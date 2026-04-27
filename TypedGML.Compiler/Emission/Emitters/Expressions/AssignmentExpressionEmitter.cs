using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class AssignmentExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (AssignmentExpressionNode)node;
        if (InstanceMemberAccessHelper.TryRenderAssignment(expression, ctx, out var instanceAssignment))
        {
            ctx.Writer.Write(instanceAssignment);
            return;
        }

        if (StaticMemberAccessHelper.TryRenderAssignment(expression, ctx, out var staticAssignment))
        {
            ctx.Writer.Write(staticAssignment);
            return;
        }

        var target = ctx.Emitter.Render(expression.Target, ctx);
        var value = ctx.Emitter.Render(expression.Value, ctx);
        if (!ExpressionSymbolHelper.IsDelegateTarget(expression.Target, ctx) ||
            expression.Op is not "+=" and not "-=")
        {
            ctx.Writer.Write($"{target} {expression.Op} {value}");
            return;
        }

        ctx.Writer.Write(expression.Op == "+="
            ? $"{target}[array_length({target})] = {value}"
            : $"{target} = __tgml_delegate_remove({target}, {value})");
    }
}
