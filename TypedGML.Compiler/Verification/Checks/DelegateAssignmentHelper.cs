using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

internal static class DelegateAssignmentHelper
{
    public static bool IsDelegateOrEventAssignment(IAstNode target, string? targetType, VerificationContext ctx)
    {
        if (DelegateTypeHelper.TrySignature(targetType, ctx, out _, out _))
            return true;

        return MemberSignatureHelper.Members(ctx.CurrentType, TargetName(target), MemberKind.Event).Any();
    }

    private static string TargetName(IAstNode target) => target switch
    {
        IdentifierExpressionNode identifier => identifier.Name,
        MemberAccessExpressionNode access => access.MemberName,
        _ => string.Empty
    };
}
