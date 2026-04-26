using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class LambdaCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is LambdaExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var lambda = (LambdaExpressionNode)node;
        if (LambdaClosureHelper.CapturesOuterScope(lambda, ctx))
            Report(DiagnosticCode.LambdaCapturesOuterScope, "Lambda cannot capture variables from the enclosing scope.", lambda.Location, DiagnosticSeverity.Error, ctx);

        if (lambda.Parameters.Any(parameter => parameter.Name == "_"))
            Report(DiagnosticCode.TypeMismatch, "Discard lambda parameters are not supported.", lambda.Location, DiagnosticSeverity.Error, ctx);

        if (lambda.Parameters.Any(parameter => string.IsNullOrEmpty(parameter.TypeRef)))
            Report(DiagnosticCode.TypeMismatch, "Lambda parameter types must be inferable from context.", lambda.Location, DiagnosticSeverity.Warning, ctx);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, DiagnosticSeverity severity, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, severity, message, location);
}
