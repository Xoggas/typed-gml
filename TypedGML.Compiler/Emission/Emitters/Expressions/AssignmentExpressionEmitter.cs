using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class AssignmentExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        ctx.Writer.Write(Render((AssignmentExpressionNode)node, ctx));
    }

    internal static string Render(AssignmentExpressionNode expression, EmitContext ctx)
    {
        if (IndexerAccessRenderer.TryRenderAssignment(expression, ctx, out var indexerAssignment))
        {
            return indexerAssignment;
        }

        var targetType = ExpressionTypeLookup.Resolve(expression.Target, ctx);
        if (ExpressionSymbolHelper.IsDelegateTarget(expression.Target, ctx) &&
            expression.Op is "+=" or "-=")
        {
            var target = ctx.Emitter.Render(expression.Target, ctx);
            return RenderDelegateAssignment(expression, target, ctx);
        }

        if (InstanceMemberAccessHelper.TryRenderAssignment(expression, ctx, out var instanceAssignment))
        {
            return instanceAssignment;
        }

        if (StaticMemberAccessHelper.TryRenderAssignment(expression, ctx, out var staticAssignment))
        {
            return staticAssignment;
        }

        var renderedValue = ctx.RenderWithExpected(expression.Value, targetType);
        UpdateNarrowing(expression, ctx);
        return $"{ctx.Emitter.Render(expression.Target, ctx)} {expression.Op} {renderedValue}";
    }

    private static string RenderDelegateAssignment(AssignmentExpressionNode expression, string target, EmitContext ctx)
    {
        var value = DelegateHandlerReferenceRenderer.Render(expression.Value, ctx);
        return expression.Op == "+="
            ? $"{target}[array_length({target})] = {value}"
            : $"{target} = __tgml_delegate_remove({target}, {value})";
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
