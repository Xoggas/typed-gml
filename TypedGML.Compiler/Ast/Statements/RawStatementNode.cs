using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record RawStatementNode(
    string GmlLine,
    SourceLocation Location) : IAstNode;
