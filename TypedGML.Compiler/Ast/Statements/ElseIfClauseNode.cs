using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record ElseIfClauseNode(
    IAstNode Condition,
    IAstNode ThenBlock,
    SourceLocation Location) : IAstNode;
