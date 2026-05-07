using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorBodyEmitter
{
    public bool NeedsBody(ConstructorDeclarationNode constructor, EmitContext ctx) =>
        constructor.Body is BlockStatementNode { Statements.Count: > 0 } ||
        constructor.ChainTarget == ConstructorChainTarget.This ||
        constructor.ChainTarget == ConstructorChainTarget.Base &&
        ctx.CurrentType?.Base?.QualifiedName != "TypedGML.GameObjects.GameObject";

    public void Emit(ConstructorDeclarationNode constructor, EmitContext ctx)
    {
        EmitBaseChain(constructor, ctx);
        EmitBody(constructor.Body, ctx);
    }

    private static void EmitBaseChain(ConstructorDeclarationNode constructor, EmitContext ctx)
    {
        if (constructor.ChainTarget == ConstructorChainTarget.Base && ctx.CurrentType?.Base is not null)
        {
            EmitBaseConstructor(ctx.CurrentType.Base, constructor.ChainArgs, ctx);
            return;
        }

        if (constructor.ChainTarget == ConstructorChainTarget.None && ctx.CurrentType?.Base is not null)
            EmitBaseConstructor(ctx.CurrentType.Base, [], ctx);
    }

    private static void EmitBaseConstructor(TypeSymbol type, IReadOnlyList<IAstNode> args, EmitContext ctx)
    {
        var chain = new List<ConstructorInlineFrame>();
        CollectBaseChain(type, args, ctx, chain);
        EmitArgumentTemps(chain, ctx);
        foreach (var frame in chain.Where(frame => frame.Body is not null))
            WithCurrentType(ctx, frame.Type, () => EmitBody(frame.Body!, ctx));
    }

    private static void CollectBaseChain(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        List<ConstructorInlineFrame> chain)
    {
        var constructor = ConstructorChainTargetResolver.Resolve(type, args, ctx, out var orderedArgs);
        if (constructor is null)
            return;

        var substitutions = ConstructorParameterSubstituter.CreateTempMap(
            constructor.Parameters,
            orderedArgs,
            ctx.NextArgumentTempVarName,
            out var temps);
        var body = ConstructorParameterSubstituter.Substitute(constructor.Body, substitutions);
        if (constructor.ChainTarget == ConstructorChainTarget.Base && type.Base is not null)
        {
            var baseArgs = ConstructorParameterSubstituter.Substitute(constructor.ChainArgs, substitutions);
            CollectBaseChain(type.Base, baseArgs, ctx, chain);
        }

        chain.Add(new ConstructorInlineFrame(type, body, temps));
    }

    private static void EmitArgumentTemps(IReadOnlyList<ConstructorInlineFrame> chain, EmitContext ctx)
    {
        foreach (var temp in chain.SelectMany(frame => frame.Temps))
        {
            ctx.Scope.Declare(temp.Name, temp.Parameter.TypeRef);
            ctx.Writer.WriteLine($"var {temp.Name} = {ctx.RenderWithExpected(temp.Value, temp.Parameter.TypeRef)};");
        }
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
