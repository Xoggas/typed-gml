using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record TryStatementNode(
    IAstNode TryBlock,
    IReadOnlyList<CatchClauseNode> CatchClauses,
    IAstNode? FinallyBlock,
    SourceLocation Location) : IAstNode;
