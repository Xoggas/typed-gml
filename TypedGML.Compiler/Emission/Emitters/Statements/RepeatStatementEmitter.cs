using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class RepeatStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is RepeatStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (RepeatStatementNode)node;
        ctx.Writer.Write($"repeat ({ctx.Emitter.Render(statement.Count, ctx)})");
        StatementEmitterHelper.EmitBody(statement.Body, ctx);
    }
}
