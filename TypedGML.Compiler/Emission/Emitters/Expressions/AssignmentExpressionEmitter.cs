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
        var targetType = ExpressionTypeLookup.Resolve(expression.Target, ctx);
        if (!ExpressionSymbolHelper.IsDelegateTarget(expression.Target, ctx) ||
            expression.Op is not "+=" and not "-=")
        {
            var renderedValue = ctx.RenderWithExpected(expression.Value, targetType);
            ctx.Writer.Write($"{target} {expression.Op} {renderedValue}");
            return;
        }

        var value = DelegateHandlerReferenceRenderer.Render(expression.Value, ctx);
        ctx.Writer.Write(expression.Op == "+="
            ? $"{target}[array_length({target})] = {value}"
            : $"{target} = __tgml_delegate_remove({target}, {value})");
    }
}
