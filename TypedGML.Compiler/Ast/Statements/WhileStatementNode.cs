using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record WhileStatementNode(
    IAstNode Condition,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
