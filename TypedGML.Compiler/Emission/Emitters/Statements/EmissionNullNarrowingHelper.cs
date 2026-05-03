using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

internal static class EmissionNullNarrowingHelper
{
    public static void WithThenBranch(IfStatementNode statement, EmitContext ctx, Action action) =>
        ctx.Narrowing.WithScope(() =>
        {
            Apply(statement.Condition, "!=", "and", ctx);
            action();
        });

    public static void ApplyEarlyExit(IfStatementNode statement, EmitContext ctx)
    {
        if (BranchAlwaysExits(statement.ThenBlock))
            Apply(statement.Condition, "==", "or", ctx);
    }

    private static void Apply(IAstNode condition, string comparisonOp, string junctionOp, EmitContext ctx)
    {
        foreach (var name in NullComparedVariables(condition, comparisonOp, junctionOp))
        {
            var typeRef = ExpressionTypeLookup.Resolve(new IdentifierExpressionNode(name, condition.Location), ctx);
            if (string.IsNullOrWhiteSpace(typeRef) || !TypeHelper.IsNullable(typeRef))
                continue;

            ctx.Narrowing.Narrow(name, TypeHelper.UnwrapNullable(typeRef));
        }
    }

    private static IEnumerable<string> NullComparedVariables(IAstNode node, string comparisonOp, string junctionOp)
    {
        if (node is BinaryExpressionNode binary && binary.Op == junctionOp)
            return NullComparedVariables(binary.Left, comparisonOp, junctionOp)
                .Concat(NullComparedVariables(binary.Right, comparisonOp, junctionOp));

        if (node is BinaryExpressionNode comparison && comparison.Op == comparisonOp)
        {
            if (TryIdentifierNullComparison(comparison.Left, comparison.Right, out var name) ||
                TryIdentifierNullComparison(comparison.Right, comparison.Left, out name))
                return [name];
        }

        return [];
    }

    private static bool TryIdentifierNullComparison(IAstNode variable, IAstNode nullValue, out string name)
    {
        if (variable is IdentifierExpressionNode identifier &&
            nullValue is LiteralExpressionNode { Kind: LiteralKind.Null })
        {
            name = identifier.Name;
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static bool BranchAlwaysExits(IAstNode node) => node switch
    {
        ReturnStatementNode or ThrowStatementNode => true,
        BlockStatementNode block => block.Statements.LastOrDefault() is ReturnStatementNode or ThrowStatementNode,
        _ => false
    };
}
