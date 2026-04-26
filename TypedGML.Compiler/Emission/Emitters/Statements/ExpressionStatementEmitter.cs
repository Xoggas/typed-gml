using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ExpressionStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ExpressionStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ExpressionStatementNode)node;
        ctx.Writer.WriteLine($"{ctx.Emitter.Render(statement.Expression, ctx)};");
    }
}
