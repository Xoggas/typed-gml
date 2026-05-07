using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorBodyEmitter
{
    public IReadOnlyList<ConstructorInlineFrame> Build(ConstructorDeclarationNode constructor, EmitContext ctx) =>
        ObjectConstructorInlineChainBuilder.ForConstructor(ctx.CurrentType, constructor, ctx);

    public IReadOnlyList<ConstructorInlineFrame> BuildImplicit(EmitContext ctx) =>
        ObjectConstructorInlineChainBuilder.ForImplicit(ctx.CurrentType, ctx);

    public bool NeedsBody(IReadOnlyList<ConstructorInlineFrame> chain) =>
        chain.Any(HasRuntimeWork);

    public void Emit(IReadOnlyList<ConstructorInlineFrame> chain, EmitContext ctx)
    {
        EmitArgumentTemps(chain, ctx);
        foreach (var frame in chain.Where(frame => frame.Body is not null))
            WithCurrentType(ctx, frame.Type, () => EmitBody(frame.Body!, ctx));
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

    private static bool HasRuntimeWork(ConstructorInlineFrame frame) =>
        frame.Temps.Count > 0 || HasStatements(frame.Body);

    private static bool HasStatements(IAstNode? body) => body switch
    {
        null => false,
        BlockStatementNode block => block.Statements.Count > 0,
        _ => true
    };

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
