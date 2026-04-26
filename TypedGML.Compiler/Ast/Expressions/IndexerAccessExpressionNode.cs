using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record IndexerAccessExpressionNode(
    IAstNode Target,
    IAstNode Index,
    SourceLocation Location) : IAstNode;
