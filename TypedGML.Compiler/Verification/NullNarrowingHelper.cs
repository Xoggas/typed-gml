using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Verification;

internal static class NullNarrowingHelper
{
    public static void ApplyThenBranch(IfStatementNode statement, VerificationContext ctx)
    {
        foreach (var narrowing in GetNullComparisons(statement.Condition, "!=", "and", ctx))
            ctx.NarrowVariable(narrowing.Key, narrowing.Value);
    }

    public static void ApplyEarlyExit(IfStatementNode statement, VerificationContext ctx)
    {
        if (statement.ElseIfClauses.Count > 0 || statement.ElseBlock is not null || !EndsWithReturnOrThrow(statement.ThenBlock))
            return;

        foreach (var narrowing in GetNullComparisons(statement.Condition, "==", "or", ctx))
            ctx.NarrowVariable(narrowing.Key, narrowing.Value);
    }

    public static string? ResolveTernary(TernaryExpressionNode expression, VerificationContext ctx, Func<IAstNode, string?> resolve)
    {
        var thenType = ResolveWithNarrowing(expression.ThenExpr, expression.Condition, "!=", ctx, resolve);
        var elseType = ResolveWithNarrowing(expression.ElseExpr, expression.Condition, "==", ctx, resolve);
        return thenType == elseType ? thenType : null;
    }

    public static void WithThenExpressionNarrowing(TernaryExpressionNode expression, VerificationContext ctx, Action action) =>
        WithNullComparisonNarrowing(expression.Condition, "!=", ctx, action);

    public static void WithElseExpressionNarrowing(TernaryExpressionNode expression, VerificationContext ctx, Action action) =>
        WithNullComparisonNarrowing(expression.Condition, "==", ctx, action);

    private static string? ResolveWithNarrowing(
        IAstNode expression,
        IAstNode condition,
        string comparisonOp,
        VerificationContext ctx,
        Func<IAstNode, string?> resolve)
    {
        string? resolved = null;
        WithNullComparisonNarrowing(condition, comparisonOp, ctx, () => resolved = resolve(expression));
        return resolved;
    }

    private static void WithNullComparisonNarrowing(IAstNode condition, string comparisonOp, VerificationContext ctx, Action action)
    {
        if (!TryGetNullComparison(condition, comparisonOp, ctx, out var name, out var typeRef))
        {
            action();
            return;
        }

        ctx.PushNarrowing(name, typeRef);
        try
        {
            action();
        }
        finally
        {
            ctx.PopNarrowing(name);
        }
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
}
