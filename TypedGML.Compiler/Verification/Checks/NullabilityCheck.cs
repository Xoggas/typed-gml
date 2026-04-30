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
        foreach (var narrowing in GetNullComparisons(statement.Condition, "!=", "and", ctx))
            ctx.NarrowVariable(narrowing.Key, narrowing.Value);
    }

    public static void ApplyEarlyExitNarrowing(IfStatementNode statement, VerificationContext ctx)
    {
        if (statement.ElseIfClauses.Count > 0 || statement.ElseBlock is not null || !EndsWithReturnOrThrow(statement.ThenBlock))
            return;

        foreach (var narrowing in GetNullComparisons(statement.Condition, "==", "or", ctx))
            ctx.NarrowVariable(narrowing.Key, narrowing.Value);
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

    private static bool TryGetNullComparison(IAstNode condition, string op, VerificationContext ctx, out string name, out string typeRef)
    {
        name = string.Empty;
        typeRef = string.Empty;

        if (condition is not BinaryExpressionNode binary || binary.Op != op)
            return false;

        var identifier = NullCheckedIdentifier(binary);
        if (identifier is null)
            return false;

        var currentType = ExpressionTypeResolver.Resolve(identifier, ctx);
        if (!TypeReferenceHelper.IsNullable(currentType))
            return false;

        name = identifier.Name;
        typeRef = TypeReferenceHelper.UnwrapNullable(currentType);
        return !string.IsNullOrWhiteSpace(typeRef);
    }

    private static IReadOnlyList<KeyValuePair<string, string>> GetNullComparisons(
        IAstNode condition,
        string comparisonOp,
        string junctionOp,
        VerificationContext ctx)
    {
        var narrowed = new List<KeyValuePair<string, string>>();
        return TryCollectNullComparisons(condition, comparisonOp, junctionOp, ctx, narrowed) ? narrowed : [];
    }

    private static bool TryCollectNullComparisons(
        IAstNode condition,
        string comparisonOp,
        string junctionOp,
        VerificationContext ctx,
        List<KeyValuePair<string, string>> narrowed)
    {
        if (condition is BinaryExpressionNode binary && binary.Op == junctionOp)
            return TryCollectNullComparisons(binary.Left, comparisonOp, junctionOp, ctx, narrowed) &&
                   TryCollectNullComparisons(binary.Right, comparisonOp, junctionOp, ctx, narrowed);

        if (!TryGetNullComparison(condition, comparisonOp, ctx, out var name, out var typeRef))
            return false;

        narrowed.Add(new KeyValuePair<string, string>(name, typeRef));
        return true;
    }

    private static IdentifierExpressionNode? NullCheckedIdentifier(BinaryExpressionNode binary) =>
        binary is { Left: IdentifierExpressionNode left, Right: LiteralExpressionNode { Kind: LiteralKind.Null } } ? left :
        binary is { Left: LiteralExpressionNode { Kind: LiteralKind.Null }, Right: IdentifierExpressionNode right } ? right :
        null;

    private static bool EndsWithReturnOrThrow(IAstNode node) => node switch
    {
        ReturnStatementNode or ThrowStatementNode => true,
        BlockStatementNode { Statements.Count: > 0 } block => block.Statements[^1] is ReturnStatementNode or ThrowStatementNode,
        _ => false
    };

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.NullAssignedToNonNullableType, DiagnosticSeverity.Error, message, location);
}
