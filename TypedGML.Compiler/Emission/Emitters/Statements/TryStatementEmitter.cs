using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class TryStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is TryStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (TryStatementNode)node;
        ctx.Writer.Write("try");
        StatementEmitterHelper.EmitBody(statement.TryBlock, ctx);

        foreach (var clause in statement.CatchClauses)
        {
            ctx.Writer.Write($"catch ({clause.VariableName})");
            StatementEmitterHelper.EmitBody(clause.Body, ctx);
        }

        if (statement.FinallyBlock is null)
            return;

        ctx.Writer.Write("finally");
        StatementEmitterHelper.EmitBody(statement.FinallyBlock, ctx);
    }
}
