using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record ExpressionStatementNode(
    IAstNode Expression,
    SourceLocation Location) : IAstNode;
