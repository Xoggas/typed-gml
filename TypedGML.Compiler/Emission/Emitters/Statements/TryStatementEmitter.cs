using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class TryStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is TryStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (TryStatementNode)node;
        ctx.Writer.Write("try");
        StatementEmitterHelper.EmitBody(statement.TryBlock, ctx);

        foreach (var clause in statement.CatchClauses)
        {
            ctx.Writer.Write($"catch ({clause.VariableName})");
            ctx.Scope.Push();
            try
            {
                ctx.Scope.Declare(clause.VariableName, ResolveCatchType(clause, ctx));
                StatementEmitterHelper.EmitBody(clause.Body, ctx);
            }
            finally
            {
                ctx.Scope.Pop();
            }
        }

        if (statement.FinallyBlock is null)
            return;

        ctx.Writer.Write("finally");
        StatementEmitterHelper.EmitBody(statement.FinallyBlock, ctx);
    }

    private static string ResolveCatchType(CatchClauseNode clause, EmitContext ctx) =>
        ExceptionNavigation.IsExceptionTypeRef(clause.ExceptionType) &&
        ExceptionNavigation.TryResolveExceptionType(ctx.Symbols, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type)
            ? type.QualifiedName
            : clause.ExceptionType;
}
