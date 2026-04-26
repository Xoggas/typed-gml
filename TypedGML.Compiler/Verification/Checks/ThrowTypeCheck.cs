using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ThrowTypeCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ThrowStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var thrown = (ThrowStatementNode)node;
        if (TypeReferenceHelper.RootName(ExpressionTypeResolver.Resolve(thrown.Expression, ctx)) != "Exception")
            ctx.Diagnostics.Report(DiagnosticCode.InvalidThrowType, DiagnosticSeverity.Error, "throw expression must be of type Exception.", thrown.Location);
    }
}
