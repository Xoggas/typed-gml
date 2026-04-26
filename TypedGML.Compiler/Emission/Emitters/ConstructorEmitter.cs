using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class ConstructorEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ConstructorDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var constructor = (ConstructorDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType)}({parameters})");
        ctx.Writer.BeginBlock();
        EmitChain(ctx, constructor);
        ctx.Dispatch(constructor.Body, ctx);
        ctx.Writer.EndBlock();
    }

    private static void EmitChain(EmitContext ctx, ConstructorDeclarationNode constructor)
    {
        var args = string.Join(", ", constructor.ChainArgs.Select(FormatArgument));
        if (constructor.ChainTarget == ConstructorChainTarget.Base && ctx.CurrentType?.Base is not null)
            ctx.Writer.WriteLine($"{NamingConvention.ConstructorName(ctx.CurrentType.Base)}({args});");
        if (constructor.ChainTarget == ConstructorChainTarget.This && ctx.CurrentType is not null)
            ctx.Writer.WriteLine($"{NamingConvention.ConstructorName(ctx.CurrentType)}({args});");
    }

    private static string FormatArgument(IAstNode node) => node switch
    {
        LiteralExpressionNode literal when literal.Kind == LiteralKind.String =>
            $"\"{literal.Value?.ToString()?.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
        LiteralExpressionNode literal when literal.Kind == LiteralKind.Null => "undefined",
        LiteralExpressionNode literal => literal.Value?.ToString() ?? "undefined",
        IdentifierExpressionNode identifier => identifier.Name,
        _ => "undefined"
    };
}
