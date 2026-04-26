using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record WithStatementNode(
    IAstNode Target,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
