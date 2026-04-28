using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class BaseCallCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is BaseCallExpressionNode or BaseAccessExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is BaseCallExpressionNode call)
            CheckCall(call, ctx);
        else
            CheckAccess((BaseAccessExpressionNode)node, ctx);
    }

    private static void CheckCall(BaseCallExpressionNode call, VerificationContext ctx)
    {
        var candidates = MemberSignatureHelper.Members(ctx.CurrentType?.Base, call.MemberName, MemberKind.Method).ToList();
        if (candidates.Count == 0)
        {
            ReportMissing(call.MemberName, call.Location, ctx);
            return;
        }

        if (!candidates.Any(candidate => Matches(candidate, call, ctx)))
            ctx.Diagnostics.Report(
                DiagnosticCode.NoMatchingMethodOverload,
                DiagnosticSeverity.Error,
                $"No overload of '{call.MemberName}' on base type '{BaseTypeName(ctx)}' matches the supplied arguments.",
                call.Location);
    }

    private static void CheckAccess(BaseAccessExpressionNode access, VerificationContext ctx)
    {
        if (MemberSignatureHelper.Members(ctx.CurrentType?.Base, access.MemberName).Any())
            return;

        ReportMissing(access.MemberName, access.Location, ctx);
    }

    private static bool Matches(MemberSymbol candidate, BaseCallExpressionNode call, VerificationContext ctx)
    {
        if (call.Args.Count < MemberSignatureHelper.RequiredParameters(candidate) ||
            call.Args.Count > candidate.Parameters.Count)
            return false;

        for (var i = 0; i < call.Args.Count; i++)
            if (!TypeReferenceHelper.IsAssignable(candidate.Parameters[i].TypeRef, ExpressionTypeResolver.Resolve(call.Args[i], ctx), ctx))
                return false;

        return true;
    }

    private static void ReportMissing(string memberName, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(
            DiagnosticCode.MissingOverrideTarget,
            DiagnosticSeverity.Error,
            $"Member '{memberName}' is not defined on base type '{BaseTypeName(ctx)}'.",
            location);

    private static string BaseTypeName(VerificationContext ctx) =>
        ctx.CurrentType?.Base?.QualifiedName ?? "<none>";
}
