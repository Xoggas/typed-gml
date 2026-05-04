using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class ConstructorEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ConstructorDeclarationNode;

    public static void EmitImplicit(TypeSymbol type, EmitContext ctx)
    {
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(type)}()");
        WithConstructorContext(ctx, [], null, () =>
        {
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine($"var {EmitContext.InstParam} = {{}};");
            ConstructorMemberInitializerEmitter.EmitDefaults(type, (IAstNode?)null, ctx);
            ctx.Writer.WriteLine($"return {EmitContext.InstParam};");
            ctx.Writer.EndBlock();
        });
    }

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var constructor = (ConstructorDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        var symbol = ResolveSymbol(ctx.CurrentType, constructor);
        var functionName = symbol is null
            ? NamingConvention.ConstructorName(ctx.CurrentType)
            : NamingConvention.ConstructorName(ctx.CurrentType, symbol);
        ctx.Writer.Write($"function {functionName}({parameters})");
        WithConstructorContext(ctx, constructor.Parameters, symbol, () =>
        {
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine($"var {EmitContext.InstParam} = {{}};");
            ConstructorChainInliner.Emit(constructor, ctx);
            ConstructorMemberInitializerEmitter.EmitDefaults(ctx.CurrentType, constructor.Body, ctx);
            EmitBodyStatements(constructor.Body, ctx);
            ctx.Writer.WriteLine($"return {EmitContext.InstParam};");
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

    private static MemberSymbol? ResolveSymbol(TypeSymbol type, ConstructorDeclarationNode constructor) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Constructor &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(constructor.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));

    private static void WithConstructorContext(
        EmitContext ctx,
        IReadOnlyList<ParameterNode> parameters,
        MemberSymbol? symbol,
        Action action)
    {
        var previousSelf = ctx.SelfName;
        var previousMember = ctx.CurrentMember;
        var previousConstructor = ctx.IsInConstructor;
        ctx.SelfName = EmitContext.InstParam;
        ctx.CurrentMember = symbol;
        ctx.IsInConstructor = true;
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
            ctx.IsInConstructor = previousConstructor;
            ctx.CurrentMember = previousMember;
            ctx.SelfName = previousSelf;
        }
    }
}
