using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record ThrowStatementNode(
    IAstNode Expression,
    SourceLocation Location) : IAstNode;
