using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class RawStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is RawStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (RawStatementNode)node;
        ctx.Writer.WriteLine(statement.GmlLine);
    }
}
