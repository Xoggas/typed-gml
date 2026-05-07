using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class ObjectCreationExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        ctx.Writer.Write(Render((ObjectCreationExpressionNode)node, ctx));
    }

    internal static string Render(ObjectCreationExpressionNode expression, EmitContext ctx)
    {
        if (!ctx.Symbols.TryResolve(expression.TypeRef, expression.TypeArgs.Count, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type))
        {
            return $"{expression.TypeRef.Replace(".", "_", StringComparison.Ordinal)}_create({ExpressionCallHelper.JoinConstructorArguments(null, expression.PositionalArgs, expression.NamedArgs, ctx)})";
        }

        if (type.ObjectAssetName is not null)
            return RenderObjectCreation(expression, type, ctx);

        if (type.GenericParameters.Count > 0 && expression.TypeArgs.Count > 0)
        {
            return RenderGenericCreation(expression, type, ctx);
        }

        return $"{ConstructorName(type, expression, ctx)}({ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx)})";
    }

    private static string RenderObjectCreation(ObjectCreationExpressionNode expression, TypeSymbol type, EmitContext ctx)
    {
        var args = ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx);
        return $"{NamingConvention.ConstructorName(type)}({args})";
    }

    private static string RenderGenericCreation(ObjectCreationExpressionNode expression, TypeSymbol type, EmitContext ctx)
    {
        var args = ExpressionCallHelper.JoinConstructorArguments(type, expression.PositionalArgs, expression.NamedArgs, ctx);
        var invocation = $"{ConstructorName(type, expression, ctx)}({args})";
        var genericArgs = GenericArgsRenderer.Render(type, expression.TypeArgs, ctx);
        return $"(function() {{ var __inst = {invocation}; __inst.__genericArgs = {genericArgs}; return __inst; }})()";
    }

    private static string ConstructorName(TypeSymbol type, ObjectCreationExpressionNode expression, EmitContext ctx)
    {
        var constructor = PickConstructor(type, expression, ctx);

        return constructor is null
            ? NamingConvention.ConstructorName(type)
            : NamingConvention.ConstructorName(type, constructor);
    }

    private static MemberSymbol? PickConstructor(TypeSymbol type, ObjectCreationExpressionNode expression, EmitContext ctx) =>
        EmissionOverloadResolver.Pick(
            type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList(),
            expression.PositionalArgs,
            expression.NamedArgs,
            ctx,
            Substitutions(type, expression.TypeArgs));

    private static IReadOnlyDictionary<string, string> Substitutions(TypeSymbol type, IReadOnlyList<string> typeArgs) =>
        type.GenericParameters.Zip(typeArgs)
            .ToDictionary(pair => pair.First.Name, pair => pair.Second, StringComparer.Ordinal);
}
