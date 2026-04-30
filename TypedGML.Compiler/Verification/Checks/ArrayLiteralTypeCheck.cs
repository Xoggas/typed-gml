using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ArrayLiteralTypeCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ArrayLiteralExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var array = (ArrayLiteralExpressionNode)node;
        var targetElementType = ArrayLiteralTargetHelper.TargetElementType(ctx.CurrentExpectedType, ctx);
        if (array.Elements.Count == 0)
            return;

        var elementType = ExpressionTypeResolver.Resolve(array.Elements[0], ctx);
        if (string.IsNullOrWhiteSpace(elementType))
            return;

        if (array.Elements.Skip(1).Any(element => !TypeReferenceHelper.IsAssignable(elementType, ExpressionTypeResolver.Resolve(element, ctx), ctx)))
        {
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, "All array literal elements must have compatible types.", array.Location);
            return;
        }

        if (targetElementType is not null && !TypeReferenceHelper.IsAssignable(targetElementType, elementType, ctx))
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Cannot assign array literal element type '{elementType}' to '{targetElementType}'.", array.Location);
    }
}
