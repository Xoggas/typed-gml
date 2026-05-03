using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class StaticCtorAssignmentFinder
{
    public static IReadOnlySet<string> FindAssignedFields(TypeSymbol type, StaticConstructorDeclarationNode? staticConstructor)
    {
        if (staticConstructor is null)
            return new HashSet<string>(StringComparer.Ordinal);

        var staticFields = type.Members
            .Where(member => member.Kind == MemberKind.Field && member.Modifiers.Contains("static", StringComparer.Ordinal))
            .Select(member => member.Name)
            .ToHashSet(StringComparer.Ordinal);
        var assignedFields = new HashSet<string>(StringComparer.Ordinal);
        var scope = new ScopeStack();
        scope.Push();
        Walk(staticConstructor.Body, type, staticFields, assignedFields, scope);
        scope.Pop();
        return assignedFields;
    }

    private static void Walk(IAstNode node, TypeSymbol type, IReadOnlySet<string> staticFields, HashSet<string> assignedFields, ScopeStack scope)
    {
        switch (node)
        {
            case AssignmentExpressionNode assignment:
                if (assignment.Op == "=" && TryGetAssignedStaticField(assignment.Target, type, staticFields, scope, out var name))
                    assignedFields.Add(name);
                Walk(assignment.Value, type, staticFields, assignedFields, scope);
                return;
            case BlockStatementNode block:
                InScope(scope, () => block.Statements.ToList().ForEach(statement => Walk(statement, type, staticFields, assignedFields, scope)));
                return;
            case VarDeclarationStatementNode declaration:
                if (declaration.Initializer is not null)
                    Walk(declaration.Initializer, type, staticFields, assignedFields, scope);
                scope.Declare(declaration.Name, declaration.TypeRef ?? string.Empty);
                return;
            case ForStatementNode loop:
                InScope(scope, () => WalkFor(loop, type, staticFields, assignedFields, scope));
                return;
            case CatchClauseNode clause:
                InScope(scope, () =>
                {
                    scope.Declare(clause.VariableName, clause.ExceptionType);
                    Walk(clause.Body, type, staticFields, assignedFields, scope);
                });
                return;
            case LambdaExpressionNode:
                return;
        }

        foreach (var child in Children(node))
            Walk(child, type, staticFields, assignedFields, scope);
    }

    private static void WalkFor(ForStatementNode loop, TypeSymbol type, IReadOnlySet<string> staticFields, HashSet<string> assignedFields, ScopeStack scope)
    {
        if (loop.Init is not null)
            Walk(loop.Init, type, staticFields, assignedFields, scope);
        if (loop.Condition is not null)
            Walk(loop.Condition, type, staticFields, assignedFields, scope);
        loop.Update.ToList().ForEach(update => Walk(update, type, staticFields, assignedFields, scope));
        Walk(loop.Body, type, staticFields, assignedFields, scope);
    }

    private static bool TryGetAssignedStaticField(IAstNode target, TypeSymbol type, IReadOnlySet<string> staticFields, ScopeStack scope, out string name)
    {
        switch (target)
        {
            case IdentifierExpressionNode identifier when staticFields.Contains(identifier.Name) && !scope.TryResolve(identifier.Name, out _):
                name = identifier.Name;
                return true;
            case MemberAccessExpressionNode access when staticFields.Contains(access.MemberName) && IsCurrentTypeReference(access.Target, type, scope):
                name = access.MemberName;
                return true;
            default:
                name = string.Empty;
                return false;
        }
    }

    private static bool IsCurrentTypeReference(IAstNode target, TypeSymbol type, ScopeStack scope)
    {
        var chain = Chain(target);
        if (chain is null || scope.TryResolve(chain.Split('.')[0], out _))
            return false;

        return string.Equals(chain, type.QualifiedName, StringComparison.Ordinal) ||
            string.Equals(chain, ShortName(type), StringComparison.Ordinal);
    }

    private static string ShortName(TypeSymbol type) =>
        type.QualifiedName.Split('.').LastOrDefault() ?? type.QualifiedName;

    private static string? Chain(IAstNode node) => node switch
    {
        IdentifierExpressionNode identifier => identifier.Name,
        MemberAccessExpressionNode access when Chain(access.Target) is { } prefix => $"{prefix}.{access.MemberName}",
        _ => null
    };

    private static void InScope(ScopeStack scope, Action action)
    {
        scope.Push();
        action();
        scope.Pop();
    }

    private static IEnumerable<IAstNode> Children(IAstNode node) => node switch
    {
        ExpressionStatementNode n => [n.Expression],
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
        RepeatStatementNode n => [n.Count, n.Body],
        WithStatementNode n => [n.Target, n.Body],
        SwitchStatementNode n => [n.Value, .. n.Sections],
        SwitchSectionNode n => [.. (n.Label is null ? [] : new[] { n.Label }), .. n.Statements],
        ThrowStatementNode n => [n.Expression],
        TryStatementNode n => [n.TryBlock, .. n.CatchClauses, .. (n.FinallyBlock is null ? [] : new[] { n.FinallyBlock })],
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
