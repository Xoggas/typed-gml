using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class WhileStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is WhileStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (WhileStatementNode)node;
        ctx.Writer.Write($"while ({ctx.Emitter.Render(statement.Condition, ctx)})");
        StatementEmitterHelper.EmitBody(statement.Body, ctx);
    }
}
