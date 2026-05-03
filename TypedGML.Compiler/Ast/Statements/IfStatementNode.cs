using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record IfStatementNode(
    IAstNode Condition,
    IAstNode ThenBlock,
    IReadOnlyList<ElseIfClauseNode> ElseIfClauses,
    IAstNode? ElseBlock,
    SourceLocation Location) : IAstNode;
