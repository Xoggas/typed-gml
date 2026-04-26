using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record IdentifierExpressionNode(
    string Name,
    SourceLocation Location) : IAstNode;
