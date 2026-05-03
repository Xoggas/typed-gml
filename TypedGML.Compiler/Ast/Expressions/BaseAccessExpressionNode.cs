using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record BaseAccessExpressionNode(
    string MemberName,
    SourceLocation Location) : IAstNode;
