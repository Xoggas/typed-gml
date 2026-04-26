using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class IfStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is IfStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (IfStatementNode)node;
        ctx.Writer.Write($"if ({ctx.Emitter.Render(statement.Condition, ctx)})");
        StatementEmitterHelper.EmitBody(statement.ThenBlock, ctx);

        foreach (var clause in statement.ElseIfClauses)
        {
            ctx.Writer.Write($"else if ({ctx.Emitter.Render(clause.Condition, ctx)})");
            StatementEmitterHelper.EmitBody(clause.ThenBlock, ctx);
        }

        if (statement.ElseBlock is null)
            return;

        ctx.Writer.Write("else");
        StatementEmitterHelper.EmitBody(statement.ElseBlock, ctx);
    }
}
