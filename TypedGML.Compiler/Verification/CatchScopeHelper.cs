using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class CatchScopeHelper
{
    public static void Walk(CatchClauseNode clause, VerificationContext ctx, Action<IAstNode> walkBody)
    {
        ctx.Scope.Push();
        try
        {
            ctx.Scope.Declare(clause.VariableName, ResolveCatchType(clause, ctx));
            walkBody(clause.Body);
        }
        finally
        {
            ctx.Scope.Pop();
        }
    }

    public static string ResolveCatchType(CatchClauseNode clause, VerificationContext ctx) =>
        ExceptionNavigation.IsExceptionTypeRef(clause.ExceptionType) &&
        ExceptionNavigation.TryResolveExceptionType(
            ctx.Symbols,
            TypeReferenceHelper.CurrentNamespace(ctx.CurrentType),
            ctx.UsingPrefixes,
            out var type)
            ? type.QualifiedName
            : clause.ExceptionType;
}
