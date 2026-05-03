using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Verification;

internal static class ReturnPathHelper
{
    public static bool ReturnsOnAllPaths(IAstNode node) => node switch
    {
        ReturnStatementNode => true,
        ThrowStatementNode => true,
        BlockStatementNode block => block.Statements.Any(ReturnsOnAllPaths),
        IfStatementNode @if => ReturnsOnAllPaths(@if.ThenBlock) &&
                               @if.ElseIfClauses.All(clause => ReturnsOnAllPaths(clause.ThenBlock)) &&
                               @if.ElseBlock is not null &&
                               ReturnsOnAllPaths(@if.ElseBlock),
        SwitchStatementNode @switch => @switch.Sections.Count > 0 &&
                                       @switch.Sections.All(section => section.Statements.LastOrDefault() is { } last && ReturnsOnAllPaths(last)),
        _ => false
    };
}
