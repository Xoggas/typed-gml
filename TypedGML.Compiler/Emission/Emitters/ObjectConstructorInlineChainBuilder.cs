using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ObjectConstructorInlineChainBuilder
{
    private const string GameObjectTypeName = "TypedGML.GameObjects.GameObject";

    public static IReadOnlyList<ConstructorInlineFrame> ForImplicit(TypeSymbol? type, EmitContext ctx)
    {
        var chain = new List<ConstructorInlineFrame>();
        if (type?.Base is not null)
            Collect(type.Base, [], ctx, chain, new HashSet<string>(StringComparer.Ordinal));
        return chain;
    }

    public static IReadOnlyList<ConstructorInlineFrame> ForConstructor(
        TypeSymbol? type,
        ConstructorDeclarationNode constructor,
        EmitContext ctx)
    {
        var chain = new List<ConstructorInlineFrame>();
        if (type is null)
            return chain;

        CollectPrefix(type, constructor.ChainTarget, constructor.ChainArgs, ctx, chain, new HashSet<string>(StringComparer.Ordinal));
        chain.Add(new ConstructorInlineFrame(type, constructor.Body, []));
        return chain;
    }

    private static void Collect(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        List<ConstructorInlineFrame> chain,
        ISet<string> visited)
    {
        if (type.QualifiedName == GameObjectTypeName)
            return;

        var constructor = ConstructorChainTargetResolver.Resolve(type, args, ctx, out var orderedArgs);
        if (constructor is null)
        {
            if (type.Base is not null)
                Collect(type.Base, [], ctx, chain, visited);
            return;
        }

        if (!visited.Add(Signature(type, constructor)))
            return;

        var substitutions = ConstructorParameterSubstituter.CreateTempMap(
            constructor.Parameters,
            orderedArgs,
            ctx.NextArgumentTempVarName,
            out var temps);
        var chainArgs = ConstructorParameterSubstituter.Substitute(constructor.ChainArgs, substitutions);
        CollectPrefix(type, constructor.ChainTarget, chainArgs, ctx, chain, visited);
        chain.Add(new ConstructorInlineFrame(type, ConstructorParameterSubstituter.Substitute(constructor.Body, substitutions), temps));
    }

    private static void CollectPrefix(
        TypeSymbol type,
        ConstructorChainTarget target,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        List<ConstructorInlineFrame> chain,
        ISet<string> visited)
    {
        if (target == ConstructorChainTarget.This)
        {
            Collect(type, args, ctx, chain, visited);
            return;
        }

        if (type.Base is not null)
            Collect(type.Base, target == ConstructorChainTarget.Base ? args : [], ctx, chain, visited);
    }

    private static string Signature(TypeSymbol type, ConstructorDeclarationNode constructor) =>
        $"{type.QualifiedName}({string.Join(",", constructor.Parameters.Select(parameter => parameter.TypeRef))})";
}
