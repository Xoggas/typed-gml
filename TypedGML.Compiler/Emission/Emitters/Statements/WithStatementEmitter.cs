using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class WithStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is WithStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (WithStatementNode)node;
        ctx.Writer.Write($"with ({ctx.Emitter.Render(statement.Target, ctx)})");
        StatementEmitterHelper.EmitBody(statement.Body, ctx);
    }
}
