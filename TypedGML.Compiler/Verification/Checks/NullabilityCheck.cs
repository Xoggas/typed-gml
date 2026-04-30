using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class NullabilityCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is AssignmentExpressionNode or VarDeclarationStatementNode or InvocationExpressionNode or MemberAccessExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case AssignmentExpressionNode assignment:
                CheckTarget(assignment.Target, ExpressionTypeResolver.Resolve(assignment.Value, ctx), assignment.Location, ctx);
                break;
            case VarDeclarationStatementNode declaration when !declaration.IsVar:
                CheckType(declaration.TypeRef, ExpressionTypeResolver.Resolve(declaration.Initializer, ctx), declaration.Location, ctx);
                break;
            case InvocationExpressionNode invocation:
                CheckInvocation(invocation, ctx);
                break;
            case MemberAccessExpressionNode access:
                CheckAccess(access, ctx);
                break;
        }
    }

    public static void ClearReassignedVariable(IAstNode node, VerificationContext ctx)
    {
        if (node is AssignmentExpressionNode { Target: IdentifierExpressionNode target })
            ctx.ClearNarrowing(target.Name);
    }

    public static void ApplyThenBranchNarrowing(IfStatementNode statement, VerificationContext ctx)
    {
        NullNarrowingHelper.ApplyThenBranch(statement, ctx);
    }

    public static void ApplyEarlyExitNarrowing(IfStatementNode statement, VerificationContext ctx)
    {
        NullNarrowingHelper.ApplyEarlyExit(statement, ctx);
    }

    private static void CheckInvocation(InvocationExpressionNode invocation, VerificationContext ctx)
    {
        if (invocation.Target is NullConditionalExpressionNode)
            return;

        var targetType = ExpressionTypeResolver.Resolve(invocation.Target, ctx);
        if (!IsMethodInvocationTarget(invocation.Target, ctx) && TypeReferenceHelper.IsNullable(targetType))
            Report("Cannot invoke a nullable value without a null-safe operator.", invocation.Location, ctx);

        if (invocation.Target is not MemberAccessExpressionNode access)
            return;

        if (TypeReferenceHelper.IsNullable(ExpressionTypeResolver.Resolve(access.Target, ctx)))
            Report("Cannot call a method on a nullable target without a null-safe operator.", invocation.Location, ctx);
    }

    private static void CheckAccess(MemberAccessExpressionNode access, VerificationContext ctx)
    {
        if (TypeReferenceHelper.IsNullable(ExpressionTypeResolver.Resolve(access.Target, ctx)))
            Report($"Cannot access member '{access.MemberName}' on a nullable target without a null-safe operator.", access.Location, ctx);
    }

    private static void CheckTarget(IAstNode target, string? sourceType, SourceLocation location, VerificationContext ctx) =>
        CheckType(ExpressionTypeResolver.Resolve(target, ctx), sourceType, location, ctx);

    private static void CheckType(string? targetType, string? sourceType, SourceLocation location, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(sourceType))
            return;

        if (!TypeReferenceHelper.IsNullable(targetType) &&
            (sourceType == "null" || TypeReferenceHelper.IsNullable(sourceType)))
            Report($"Cannot use nullable value '{sourceType}' as non-nullable type '{targetType}'.", location, ctx);
    }

    private static bool IsMethodInvocationTarget(IAstNode target, VerificationContext ctx) => target switch
    {
        IdentifierExpressionNode identifier => SymbolResolver.FindMember(ctx.CurrentType, identifier.Name, out _)?.Kind == MemberKind.Method,
        MemberAccessExpressionNode access => TryResolveAccessMember(access, ctx, out var member) && member.Kind == MemberKind.Method,
        _ => false
    };

    private static bool TryResolveAccessMember(MemberAccessExpressionNode access, VerificationContext ctx, out MemberSymbol member)
    {
        member = null!;
        if (QualifiedTypeAccessResolver.TryResolveMember(access, ctx, out var owner, out var memberName))
            member = SymbolResolver.FindMember(PrimitiveBclTypeResolver.ResolveMemberOwner(owner, ctx.Symbols), memberName, out _)!;
        else if (SymbolResolver.TryResolveType(TypeReferenceHelper.UnwrapNullable(ExpressionTypeResolver.Resolve(access.Target, ctx)), ctx, out var targetType))
            member = SymbolResolver.FindMember(targetType, access.MemberName, out _)!;

        return member is not null;
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.NullAssignedToNonNullableType, DiagnosticSeverity.Error, message, location);
}
