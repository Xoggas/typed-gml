using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Verification;

internal static class LambdaClosureHelper
{
    public static bool CapturesOuterScope(LambdaExpressionNode lambda, VerificationContext ctx)
    {
        var scopes = new Stack<HashSet<string>>();
        scopes.Push(lambda.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal));
        return Walk(lambda.Body, scopes, ctx);
    }

    private static bool Walk(IAstNode node, Stack<HashSet<string>> scopes, VerificationContext ctx) => node switch
    {
        IdentifierExpressionNode id => Captures(id.Name, scopes, ctx),
        BlockStatementNode block => InScope(scopes, () => block.Statements.Any(statement => Walk(statement, scopes, ctx))),
        VarDeclarationStatementNode decl => DeclareThenWalk(decl, scopes, ctx),
        CatchClauseNode clause => InScope(scopes, () => { scopes.Peek().Add(clause.VariableName); return Walk(clause.Body, scopes, ctx); }),
        LambdaExpressionNode => false,
        _ => Children(node).Any(child => Walk(child, scopes, ctx))
    };

    private static bool DeclareThenWalk(VarDeclarationStatementNode decl, Stack<HashSet<string>> scopes, VerificationContext ctx)
    {
        if (decl.Initializer is not null && Walk(decl.Initializer, scopes, ctx))
            return true;

        scopes.Peek().Add(decl.Name);
        return false;
    }

    private static bool Captures(string name, Stack<HashSet<string>> scopes, VerificationContext ctx) =>
        name != "this" &&
        name != "base" &&
        name != "field" &&
        name != "value" &&
        scopes.All(scope => !scope.Contains(name)) &&
        ctx.Scope.TryResolve(name, out _);

    private static bool InScope(Stack<HashSet<string>> scopes, Func<bool> action)
    {
        scopes.Push(new HashSet<string>(StringComparer.Ordinal));
        var result = action();
        scopes.Pop();
        return result;
    }

    private static IEnumerable<IAstNode> Children(IAstNode node) => node switch
    {
        ExpressionStatementNode n => [n.Expression],
        AssignmentExpressionNode n => [n.Target, n.Value],
        BinaryExpressionNode n => [n.Left, n.Right],
        UnaryExpressionNode n => [n.Operand],
        TernaryExpressionNode n => [n.Condition, n.ThenExpr, n.ElseExpr],
        MemberAccessExpressionNode n => [n.Target],
        IndexerAccessExpressionNode n => [n.Target, n.Index],
        InvocationExpressionNode n => [n.Target, .. n.PositionalArgs, .. n.NamedArgs],
        NamedArgNode n => [n.Value],
        ReturnStatementNode n when n.Value is not null => [n.Value],
        IfStatementNode n => [n.Condition, n.ThenBlock, .. n.ElseIfClauses, .. (n.ElseBlock is null ? [] : new[] { n.ElseBlock })],
        ElseIfClauseNode n => [n.Condition, n.ThenBlock],
        WhileStatementNode n => [n.Condition, n.Body],
        ForStatementNode n => [.. (n.Init is null ? [] : new[] { n.Init }), .. (n.Condition is null ? [] : new[] { n.Condition }), .. n.Update, n.Body],
        RepeatStatementNode n => [n.Count, n.Body],
        WithStatementNode n => [n.Target, n.Body],
        SwitchStatementNode n => [n.Value, .. n.Sections],
        SwitchSectionNode n => [.. (n.Label is null ? [] : new[] { n.Label }), .. n.Statements],
        ThrowStatementNode n => [n.Expression],
        TryStatementNode n => [n.TryBlock, .. n.CatchClauses, .. (n.FinallyBlock is null ? [] : new[] { n.FinallyBlock })],
        CatchClauseNode n => [n.Body],
        ArrayLiteralExpressionNode n => n.Elements,
        DictionaryLiteralExpressionNode n => n.Entries,
        DictionaryEntryNode n => [n.Key, n.Value],
        CastExpressionNode n => [n.Expression],
        NullCoalescingExpressionNode n => [n.Left, n.Right],
        NullConditionalExpressionNode n => [n.Target],
        ObjectCreationExpressionNode n => [.. n.PositionalArgs, .. n.NamedArgs],
        _ => []
    };
}
