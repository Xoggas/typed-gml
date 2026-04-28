using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission;

public sealed class NodeEmitterFacade(Action<IAstNode, EmitContext> dispatch)
{
    public void Emit(IAstNode node, EmitContext ctx)
    {
        if (GmlExpressionRenderer.CanRender(node))
        {
            ctx.Writer.Write(GmlExpressionRenderer.Render(node, ctx));
            return;
        }

        if (node is BreakStatementNode)
        {
            ctx.Writer.WriteLine("break;");
            return;
        }

        if (node is ContinueStatementNode)
        {
            ctx.Writer.WriteLine("continue;");
            return;
        }

        dispatch(node, ctx);
    }

    public string Render(IAstNode? node, EmitContext ctx) => node switch
    {
        null => string.Empty,
        VarDeclarationStatementNode varDecl => RenderVar(varDecl, ctx),
        ExpressionStatementNode expr => Render(expr.Expression, ctx),
        RawStatementNode raw => raw.GmlLine.Trim().TrimEnd(';'),
        _ when GmlExpressionRenderer.CanRender(node) => DispatchOrFallback(node, ctx),
        _ => string.Empty
    };

    private string DispatchOrFallback(IAstNode node, EmitContext ctx)
    {
        var writer = new GmlWriter();
        var nested = ctx.WithWriter(writer);

        dispatch(node, nested);
        var rendered = writer.GetOutput().TrimEnd();
        return string.IsNullOrEmpty(rendered) ? GmlExpressionRenderer.Render(node, ctx) : rendered;
    }

    private static string RenderVar(VarDeclarationStatementNode declaration, EmitContext ctx)
    {
        var initializer = declaration.Initializer is null ? string.Empty : $" = {ctx.Emitter.Render(declaration.Initializer, ctx)}";
        return $"var {declaration.Name}{initializer}";
    }
}
