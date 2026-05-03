using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ExpressionStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ExpressionStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ExpressionStatementNode)node;
        if (statement.Expression is BaseCallExpressionNode baseCall)
        {
            BaseCallInlineRenderer.Emit(baseCall, ctx);
            return;
        }

        var expression = ctx.RenderWithTempPrelude(statement.Expression);
        ctx.FlushTempPrelude();
        ctx.Writer.WriteLine($"{expression};");
    }
}
