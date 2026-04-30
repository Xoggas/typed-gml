using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class ObjectCreationExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (ObjectCreationExpressionNode)node;
        if (!ctx.Symbols.TryResolve(expression.TypeRef, expression.TypeArgs.Count, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type))
        {
            ctx.Writer.Write($"{expression.TypeRef.Replace(".", "_", StringComparison.Ordinal)}_create({ExpressionCallHelper.JoinConstructorArguments(null, expression.PositionalArgs, expression.NamedArgs, ctx)})");
            return;
        }

        if (type.GenericParameters.Count > 0 && expression.TypeArgs.Count > 0)
        {
            EmitGenericCreation(expression, type, ctx);
            return;
        }

        if (type.ObjectAssetName is null)
        {
            ctx.Writer.Write($"{ConstructorName(type, expression, ctx)}({ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx)})");
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

        ctx.Writer.Write($"{ConstructorName(type, expression, ctx)}({string.Join(", ", expression.PositionalArgs.Select(a => ctx.Emitter.Render(a, ctx)).Concat(expression.NamedArgs.Select(a => ctx.Emitter.Render(a.Value, ctx))) )})");
    }

    private static void EmitGenericCreation(ObjectCreationExpressionNode expression, TypeSymbol type, EmitContext ctx)
    {
        var args = ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx);
        var invocation = $"{ConstructorName(type, expression, ctx)}({args})";
        var genericArgs = GenericArgsRenderer.Render(type, expression.TypeArgs, ctx);
        ctx.Writer.Write($"(function() {{ var __inst = {invocation}; __inst.__genericArgs = {genericArgs}; return __inst; }})()");
    }

    private static string ConstructorName(TypeSymbol type, ObjectCreationExpressionNode expression, EmitContext ctx)
    {
        var constructor = EmissionOverloadResolver.Pick(
            type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList(),
            expression.PositionalArgs,
            expression.NamedArgs,
            ctx,
            Substitutions(type, expression.TypeArgs));

        return constructor is null
            ? NamingConvention.ConstructorName(type)
            : NamingConvention.ConstructorName(type, constructor);
    }

    private static IReadOnlyDictionary<string, string> Substitutions(TypeSymbol type, IReadOnlyList<string> typeArgs) =>
        type.GenericParameters.Zip(typeArgs)
            .ToDictionary(pair => pair.First.Name, pair => pair.Second, StringComparer.Ordinal);
}
