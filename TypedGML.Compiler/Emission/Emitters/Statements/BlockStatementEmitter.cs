using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class BlockStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is BlockStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var block = (BlockStatementNode)node;
        ctx.Writer.BeginBlock();
        StatementEmitterHelper.EmitStatements(block.Statements, ctx);
        ctx.Writer.EndBlock();
    }
}
