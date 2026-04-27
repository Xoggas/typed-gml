using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;

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
        WithConstructorContext(ctx, constructor.Parameters, () =>
        {
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine("var self = {};");
            EmitFieldInitializers(ctx);
            EmitChain(ctx, constructor);
            EmitBodyStatements(constructor.Body, ctx);
            ctx.Writer.WriteLine("return self;");
            ctx.Writer.EndBlock();
        });
    }

    private static void EmitBodyStatements(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode block)
        {
            foreach (var stmt in block.Statements)
                ctx.Dispatch(stmt, ctx);
            return;
        }
        ctx.Dispatch(body, ctx);
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

    private static void EmitFieldInitializers(EmitContext ctx)
    {
        foreach (var field in Fields(ctx.CurrentType))
            ctx.Writer.WriteLine($"self.{field.Name} = undefined;");
    }

    private static IEnumerable<Symbols.MemberSymbol> Fields(Symbols.TypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.Base)
            foreach (var field in current.Members.Where(m =>
                         m.Kind == Symbols.MemberKind.Field &&
                         !m.Modifiers.Contains("static", StringComparer.Ordinal) &&
                         !m.Modifiers.Contains("const", StringComparer.Ordinal)))
                yield return field;
    }

    private static void WithConstructorContext(EmitContext ctx, IReadOnlyList<ParameterNode> parameters, Action action)
    {
        var previousSelf = ctx.SelfName;
        ctx.SelfName = "self";
        ctx.Scope.Push();
        foreach (var parameter in parameters)
            ctx.Scope.Declare(parameter.Name, parameter.TypeRef);
        try
        {
            action();
        }
        finally
        {
            ctx.Scope.Pop();
            ctx.SelfName = previousSelf;
        }
    }
}
