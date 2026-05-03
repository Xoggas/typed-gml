using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record TypeofExpressionNode(
    string TypeName,
    SourceLocation Location) : IAstNode;
