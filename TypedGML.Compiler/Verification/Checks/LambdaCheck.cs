using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
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

        if (!DelegateTypeHelper.TrySignature(ctx.CurrentExpectedType, ctx, out var returnType, out var parameterTypes))
        {
            if (lambda.Parameters.Any(parameter => string.IsNullOrEmpty(parameter.TypeRef)))
                Report(DiagnosticCode.TypeMismatch, "Lambda parameter types must be inferable from context.", lambda.Location, DiagnosticSeverity.Warning, ctx);
            return;
        }

        CheckParameters(lambda, parameterTypes, ctx);
        CheckReturn(lambda, returnType, ctx);
    }

    private static void CheckParameters(LambdaExpressionNode lambda, IReadOnlyList<string> parameterTypes, VerificationContext ctx)
    {
        if (lambda.Parameters.Count != parameterTypes.Count)
        {
            Report(DiagnosticCode.DelegateSignatureMismatch, "Lambda parameter count does not match the target delegate signature.", lambda.Location, DiagnosticSeverity.Error, ctx);
            return;
        }

        foreach (var pair in lambda.Parameters.Zip(parameterTypes))
            if (!string.IsNullOrEmpty(pair.First.TypeRef) && pair.First.TypeRef != pair.Second)
                Report(DiagnosticCode.DelegateSignatureMismatch, "Lambda parameter type does not match the target delegate signature.", pair.First.Location, DiagnosticSeverity.Error, ctx);
    }

    private static void CheckReturn(LambdaExpressionNode lambda, string returnType, VerificationContext ctx)
    {
        if (returnType == "void")
            return;

        if (lambda.Body is BlockStatementNode block)
        {
            if (!ReturnPathHelper.ReturnsOnAllPaths(block))
                Report(DiagnosticCode.MissingReturnInNonVoidMethod, $"Not all code paths return a value of type '{returnType}'.", lambda.Location, DiagnosticSeverity.Error, ctx);
            return;
        }

        var valueType = ExpressionTypeResolver.Resolve(lambda.Body, ctx);
        if (!TypeReferenceHelper.IsAssignable(returnType, valueType, ctx))
            Report(DiagnosticCode.TypeMismatch, $"Cannot return '{valueType ?? "unknown"}' from a lambda returning '{returnType}'.", lambda.Location, DiagnosticSeverity.Error, ctx);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, DiagnosticSeverity severity, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, severity, message, location);
}
