using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ForStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ForStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ForStatementNode)node;
        var init = ctx.Emitter.Render(statement.Init, ctx);
        var condition = RenderCondition(statement.Condition, ctx);
        var update = string.Join(", ", statement.Update.Select(n => ctx.Emitter.Render(n, ctx)));
        ctx.FlushTempPrelude();
        ctx.Writer.Write($"for ({init}; {condition}; {update})");
        StatementEmitterHelper.EmitBody(statement.Body, ctx);
    }

    private static string RenderCondition(IAstNode? condition, EmitContext ctx)
    {
        var rendered = ctx.Emitter.Render(condition, ctx);
        return condition is BinaryExpressionNode && rendered.Length >= 2 && rendered[0] == '(' && rendered[^1] == ')'
            ? rendered[1..^1]
            : rendered;
    }
}
