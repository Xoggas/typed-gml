using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record RepeatStatementNode(
    IAstNode Count,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
