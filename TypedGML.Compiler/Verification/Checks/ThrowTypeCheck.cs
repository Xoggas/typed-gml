using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ThrowTypeCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ThrowStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var thrown = (ThrowStatementNode)node;
        if (!ExceptionNavigation.IsExceptionTypeRef(ExpressionTypeResolver.Resolve(thrown.Expression, ctx)))
            ctx.Diagnostics.Report(DiagnosticCode.InvalidThrowType, DiagnosticSeverity.Error, "throw expression must be of type Exception.", thrown.Location);
    }
}
