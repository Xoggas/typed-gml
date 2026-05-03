using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record ReturnStatementNode(
    IAstNode? Value,
    SourceLocation Location) : IAstNode;
