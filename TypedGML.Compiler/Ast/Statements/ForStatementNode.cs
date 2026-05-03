using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record ForStatementNode(
    IAstNode? Init,
    IAstNode? Condition,
    IReadOnlyList<IAstNode> Update,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
