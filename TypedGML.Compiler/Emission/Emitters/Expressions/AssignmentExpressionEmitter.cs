using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class AssignmentExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (AssignmentExpressionNode)node;
        if (IndexerAccessRenderer.TryRenderAssignment(expression, ctx, out var indexerAssignment))
        {
            ctx.Writer.Write(indexerAssignment);
            return;
        }

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
            UpdateNarrowing(expression, ctx);
            return;
        }

        var value = DelegateHandlerReferenceRenderer.Render(expression.Value, ctx);
        ctx.Writer.Write(expression.Op == "+="
            ? $"{target}[array_length({target})] = {value}"
            : $"{target} = __tgml_delegate_remove({target}, {value})");
    }

    private static void UpdateNarrowing(AssignmentExpressionNode expression, EmitContext ctx)
    {
        if (expression.Op != "=" ||
            expression.Target is not IdentifierExpressionNode identifier ||
            !ctx.Scope.TryResolve(identifier.Name, out _))
            return;

        ctx.Narrowing.Clear(identifier.Name);
        if (expression.Value is CastExpressionNode { CastKind: CastKind.As } cast)
            ctx.Narrowing.Narrow(identifier.Name, cast.TargetType);
    }
}
