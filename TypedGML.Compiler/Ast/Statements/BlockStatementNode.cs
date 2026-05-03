using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record BlockStatementNode(
    IReadOnlyList<IAstNode> Statements,
    SourceLocation Location) : IAstNode;
