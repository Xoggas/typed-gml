using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ThrowStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ThrowStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ThrowStatementNode)node;
        ctx.Writer.WriteLine($"throw {ctx.Emitter.Render(statement.Expression, ctx)};");
    }
}
