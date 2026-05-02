using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorChainInliner
{
    public static void Emit(ConstructorDeclarationNode constructor, EmitContext ctx)
    {
        if (constructor.ChainTarget == ConstructorChainTarget.Base && ctx.CurrentType?.Base is not null)
        {
            EmitBaseConstructor(ctx.CurrentType.Base, constructor.ChainArgs, ctx);
            return;
        }

        if (constructor.ChainTarget == ConstructorChainTarget.This && ctx.CurrentType is not null)
        {
            var positionalArgs = CallArgumentOrderer.PositionalFromMixed(constructor.ChainArgs);
            var namedArgs = CallArgumentOrderer.NamedFromMixed(constructor.ChainArgs);
            var symbol = EmissionOverloadResolver.Pick(
                ctx.CurrentType.Members.Where(member => member.Kind == MemberKind.Constructor).ToList(),
                positionalArgs,
                namedArgs,
                ctx);
            var orderedArgs = symbol is not null &&
                CallArgumentOrderer.TryOrder(symbol, positionalArgs, namedArgs, true, out var ordered)
                    ? ordered
                    : positionalArgs.Concat(namedArgs.Select(arg => arg.Value)).ToList();
            var args = string.Join(", ", orderedArgs.Select(arg => ctx.Emitter.Render(arg, ctx)));
            var name = symbol is null
                ? NamingConvention.ConstructorName(ctx.CurrentType)
                : NamingConvention.ConstructorName(ctx.CurrentType, symbol);
            ctx.Writer.WriteLine($"{name}({args});");
        }
    }

    private static void EmitBaseConstructor(TypeSymbol type, IReadOnlyList<IAstNode> args, EmitContext ctx)
    {
        var chain = new List<(TypeSymbol Type, IAstNode? Body)>();
        CollectBaseChain(type, args, ctx, chain);
        EmitDefaultInitializers(chain, ctx);
        EmitBodies(chain, ctx);
    }

    private static void CollectBaseChain(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        List<(TypeSymbol Type, IAstNode? Body)> chain)
    {
        var constructor = ConstructorChainTargetResolver.Resolve(type, args, ctx, out var orderedArgs);
        if (constructor is null)
        {
            CollectDefaultInitializerChain(type.Base, chain);
            chain.Add((type, null));
            return;
        }

        if (constructor.ChainTarget == ConstructorChainTarget.Base && type.Base is not null)
        {
            var baseArgs = ConstructorParameterSubstituter.Substitute(constructor.ChainArgs, constructor.Parameters, orderedArgs);
            CollectBaseChain(type.Base, baseArgs, ctx, chain);
        }
        else
        {
            CollectDefaultInitializerChain(type.Base, chain);
        }

        var body = ConstructorParameterSubstituter.Substitute(constructor.Body, constructor.Parameters, orderedArgs);
        chain.Add((type, body));
    }

    private static void CollectDefaultInitializerChain(
        TypeSymbol? type,
        List<(TypeSymbol Type, IAstNode? Body)> chain)
    {
        if (type is null)
            return;

        CollectDefaultInitializerChain(type.Base, chain);
        chain.Add((type, null));
    }

    private static void EmitDefaultInitializers(
        IReadOnlyList<(TypeSymbol Type, IAstNode? Body)> chain,
        EmitContext ctx)
    {
        foreach (var frame in chain)
            WithCurrentType(ctx, frame.Type, () =>
            {
                ConstructorFieldInitializerEmitter.EmitAll(frame.Type, ctx);
                ConstructorAutoPropertyInitializerEmitter.Emit(frame.Type, ctx);
            });
    }

    private static void EmitBodies(
        IReadOnlyList<(TypeSymbol Type, IAstNode? Body)> chain,
        EmitContext ctx)
    {
        foreach (var frame in chain.Where(frame => frame.Body is not null))
            WithCurrentType(ctx, frame.Type, () => EmitBody(frame.Body!, ctx));
    }

    private static void EmitBody(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode block)
        {
            foreach (var statement in block.Statements)
                ctx.Dispatch(statement, ctx);
            return;
        }

        ctx.Dispatch(body, ctx);
    }

    private static void WithCurrentType(EmitContext ctx, TypeSymbol type, Action action)
    {
        var previousType = ctx.CurrentType;
        ctx.CurrentType = type;
        try
        {
            action();
        }
        finally
        {
            ctx.CurrentType = previousType;
        }
    }
}
