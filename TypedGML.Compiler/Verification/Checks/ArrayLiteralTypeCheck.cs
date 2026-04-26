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
        if (array.Elements.Count == 0)
        {
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, "Empty array literal requires an explicit type context.", array.Location);
            return;
        }

        var elementType = ExpressionTypeResolver.Resolve(array.Elements[0], ctx);
        if (array.Elements.Skip(1).Any(element => !TypeReferenceHelper.IsAssignable(elementType, ExpressionTypeResolver.Resolve(element, ctx), ctx)))
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, "All array literal elements must have compatible types.", array.Location);
    }
}
