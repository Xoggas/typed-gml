using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

internal static class StatementEmitterHelper
{
    public static void EmitBody(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode)
        {
            ctx.Emitter.Emit(body, ctx);
            return;
        }

        ctx.Writer.BeginBlock();
        EmitStatement(body, ctx);
        ctx.Writer.EndBlock();
    }

    public static void EmitStatements(IEnumerable<IAstNode> statements, EmitContext ctx)
    {
        foreach (var statement in statements)
            EmitStatement(statement, ctx);
    }

    private static void EmitStatement(IAstNode statement, EmitContext ctx)
    {
        if (statement is BreakStatementNode)
        {
            ctx.Writer.WriteLine("break;");
            return;
        }

        if (statement is ContinueStatementNode)
        {
            ctx.Writer.WriteLine("continue;");
            return;
        }

        ctx.Emitter.Emit(statement, ctx);
    }
}
