using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class IsAsCompatibilityCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is CastExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var cast = (CastExpressionNode)node;
        var sourceType = ExpressionTypeResolver.Resolve(cast.Expression, ctx);
        if (!TypeReferenceHelper.AreRelated(sourceType, cast.TargetType, ctx))
            ctx.Diagnostics.Report(
                DiagnosticCode.TypeMismatch,
                DiagnosticSeverity.Warning,
                cast.CastKind == CastKind.Is
                    ? $"is-check between unrelated types '{sourceType}' and '{cast.TargetType}' is always false."
                    : $"as-cast between unrelated types '{sourceType}' and '{cast.TargetType}' is always invalid.",
                cast.Location);
    }
}
