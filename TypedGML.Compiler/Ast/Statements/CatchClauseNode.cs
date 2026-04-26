using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record CatchClauseNode(
    string ExceptionType,
    string VariableName,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
