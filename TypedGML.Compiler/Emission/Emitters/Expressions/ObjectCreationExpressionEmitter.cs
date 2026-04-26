using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class ObjectCreationExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (ObjectCreationExpressionNode)node;
        if (!ExpressionSymbolHelper.TryResolveType(ctx, expression.TypeRef, out var type))
        {
            ctx.Writer.Write($"{expression.TypeRef.Replace(".", "_", StringComparison.Ordinal)}_create({ExpressionCallHelper.JoinConstructorArguments(null, expression.PositionalArgs, expression.NamedArgs, ctx)})");
            return;
        }

        if (type.ObjectAssetName is null)
        {
            ctx.Writer.Write($"{NamingConvention.ConstructorName(type)}({ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx)})");
            return;
        }

        if (expression.PositionalArgs.Count <= 3)
        {
            var x = expression.PositionalArgs.ElementAtOrDefault(0) is null ? "undefined" : ctx.Emitter.Render(expression.PositionalArgs[0], ctx);
            var y = expression.PositionalArgs.ElementAtOrDefault(1) is null ? "undefined" : ctx.Emitter.Render(expression.PositionalArgs[1], ctx);
            var layer = expression.PositionalArgs.ElementAtOrDefault(2) is null ? "undefined" : ctx.Emitter.Render(expression.PositionalArgs[2], ctx);
            ctx.Writer.Write($"instance_create_layer({x}, {y}, {layer}, {type.ObjectAssetName})");
            return;
        }

        ctx.Writer.Write($"{NamingConvention.ConstructorName(type)}({string.Join(", ", expression.PositionalArgs.Select(a => ctx.Emitter.Render(a, ctx)).Concat(expression.NamedArgs.Select(a => ctx.Emitter.Render(a.Value, ctx))) )})");
    }
}
