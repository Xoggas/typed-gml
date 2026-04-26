using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ReturnStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ReturnStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ReturnStatementNode)node;
        var value = statement.Value is null ? string.Empty : $" {ctx.Emitter.Render(statement.Value, ctx)}";
        ctx.Writer.WriteLine($"return{value};");
    }
}
