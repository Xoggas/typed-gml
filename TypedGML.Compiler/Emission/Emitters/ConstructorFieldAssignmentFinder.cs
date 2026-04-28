using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorFieldAssignmentFinder
{
    public static HashSet<string> Find(IAstNode body)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        Collect(body, fields);
        return fields;
    }

    private static void Collect(IAstNode? node, HashSet<string> fields)
    {
        switch (node)
        {
            case null:
                return;
            case AssignmentExpressionNode assignment:
                AddTarget(assignment.Target, fields);
                Collect(assignment.Value, fields);
                break;
            case BlockStatementNode block:
                foreach (var statement in block.Statements)
                    Collect(statement, fields);
                break;
            case ExpressionStatementNode statement:
                Collect(statement.Expression, fields);
                break;
            case IfStatementNode @if:
                Collect(@if.ThenBlock, fields);
                foreach (var clause in @if.ElseIfClauses)
                    Collect(clause.ThenBlock, fields);
                Collect(@if.ElseBlock, fields);
                break;
        }
    }

    private static void AddTarget(IAstNode target, HashSet<string> fields)
    {
        switch (target)
        {
            case IdentifierExpressionNode identifier:
                fields.Add(identifier.Name);
                break;
            case MemberAccessExpressionNode { Target: ThisExpressionNode, MemberName: var memberName }:
                fields.Add(memberName);
                break;
        }
    }
}
