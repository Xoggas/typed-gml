using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class IfStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is IfStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (IfStatementNode)node;
        var condition = ctx.RenderWithTempPrelude(statement.Condition);
        ctx.FlushTempPrelude();
        ctx.Writer.Write($"if ({condition})");
        EmissionNullNarrowingHelper.WithThenBranch(
            statement,
            ctx,
            () => StatementEmitterHelper.EmitBody(statement.ThenBlock, ctx));

        foreach (var clause in statement.ElseIfClauses)
        {
            var elseIfCondition = ctx.RenderWithTempPrelude(clause.Condition);
            ctx.FlushTempPrelude();
            ctx.Writer.Write($"else if ({elseIfCondition})");
            StatementEmitterHelper.EmitBody(clause.ThenBlock, ctx);
        }

        if (statement.ElseBlock is null)
        {
            EmissionNullNarrowingHelper.ApplyEarlyExit(statement, ctx);
            return;
        }

        ctx.Writer.Write("else");
        StatementEmitterHelper.EmitBody(statement.ElseBlock, ctx);
    }
}
