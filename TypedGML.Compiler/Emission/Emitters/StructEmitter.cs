using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class StructEmitter(StaticCtorEmitter staticCtorEmitter) : INodeEmitter
{
    public bool Matches(IAstNode node) => node is StructDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var declaration = (StructDeclarationNode)node;
        var previousType = ctx.CurrentType;
        ctx.CurrentType = ResolveType(ctx, declaration.Name, declaration.GenericParams.Count);
        EmitCreate(declaration, ctx);
        foreach (var member in declaration.Members.Where(m => m is not ConstructorDeclarationNode))
            ctx.Dispatch(member, ctx);
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
        ctx.CurrentType = previousType;
    }

    private static void EmitCreate(StructDeclarationNode declaration, EmitContext ctx)
    {
        var constructor = declaration.Members.OfType<ConstructorDeclarationNode>().FirstOrDefault();
        var parameters = constructor is null ? string.Empty : string.Join(", ", constructor.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}({parameters})");
        WithConstructorContext(ctx, constructor?.Parameters ?? [], () =>
        {
            ctx.Writer.BeginBlock();

            ctx.Writer.WriteLine($"var {EmitContext.InstParam} = {{}};");
            if (constructor is null)
                ConstructorMemberInitializerEmitter.EmitDefaults(ctx.CurrentType, (IAstNode?)null, ctx);
            else
            {
                ConstructorMemberInitializerEmitter.EmitDefaults(ctx.CurrentType, constructor.Body, ctx);
                EmitConstructorStatements(constructor.Body, ctx);
            }

            ctx.Writer.WriteLine($"return {EmitContext.InstParam};");
            ctx.Writer.EndBlock();
        });
    }

    private static void EmitConstructorStatements(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode block)
        {
            foreach (var stmt in block.Statements)
                ctx.Dispatch(stmt, ctx);
            return;
        }
        ctx.Dispatch(body, ctx);
    }

    private static TypeSymbol? ResolveType(EmitContext ctx, string name, int arity) =>
        ctx.Symbols.TryResolve(name, arity, ctx.CurrentNamespacePrefix, [], out var symbol) ? symbol : null;

    private static void WithConstructorContext(EmitContext ctx, IReadOnlyList<ParameterNode> parameters, Action action)
    {
        var previousSelf = ctx.SelfName;
        ctx.SelfName = EmitContext.InstParam;
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
