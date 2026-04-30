using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;

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
            var args = string.Join(", ", constructor.ChainArgs.Select(arg => ctx.Emitter.Render(arg, ctx)));
            var symbol = EmissionOverloadResolver.Pick(
                ctx.CurrentType.Members.Where(member => member.Kind == MemberKind.Constructor).ToList(),
                constructor.ChainArgs,
                [],
                ctx);
            var name = symbol is null
                ? NamingConvention.ConstructorName(ctx.CurrentType)
                : NamingConvention.ConstructorName(ctx.CurrentType, symbol);
            ctx.Writer.WriteLine($"{name}({args});");
        }
    }

    private static void EmitBaseConstructor(TypeSymbol type, IReadOnlyList<IAstNode> args, EmitContext ctx)
    {
        var constructor = ResolveConstructor(type, args.Count, ctx);
        if (constructor is null)
        {
            WithCurrentType(ctx, type, () => ConstructorFieldInitializerEmitter.Emit(type, null, ctx));
            return;
        }

        var body = ConstructorParameterSubstituter.Substitute(constructor.Body, constructor.Parameters, args);
        WithCurrentType(ctx, type, () =>
        {
            if (constructor.ChainTarget == ConstructorChainTarget.Base && type.Base is not null)
            {
                var baseArgs = ConstructorParameterSubstituter.Substitute(constructor.ChainArgs, constructor.Parameters, args);
                EmitBaseConstructor(type.Base, baseArgs, ctx);
            }

            ConstructorFieldInitializerEmitter.Emit(type, body, ctx);
            EmitBody(body, ctx);
        });
    }

    private static ConstructorDeclarationNode? ResolveConstructor(TypeSymbol type, int argumentCount, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var declaration) || declaration is not ClassDeclarationNode @class)
            return null;

        return @class.Members.OfType<ConstructorDeclarationNode>()
            .FirstOrDefault(constructor =>
                argumentCount >= constructor.Parameters.Count(parameter => parameter.DefaultValue is null) &&
                argumentCount <= constructor.Parameters.Count);
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
