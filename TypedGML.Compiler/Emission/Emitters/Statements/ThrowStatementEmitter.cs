using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class ThrowStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ThrowStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (ThrowStatementNode)node;
        if (statement.Expression is ObjectCreationExpressionNode creation &&
            creation.PositionalArgs.Count > 0 &&
            ExceptionNavigation.IsExceptionTypeRef(creation.TypeRef))
        {
            ctx.Writer.WriteLine($"throw {ctx.Emitter.Render(creation.PositionalArgs[0], ctx)};");
            return;
        }

        ctx.Writer.WriteLine($"throw {ctx.Emitter.Render(statement.Expression, ctx)};");
    }
}
