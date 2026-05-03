using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record DocCommentNode(
    IReadOnlyList<string> Lines,
    SourceLocation Location) : IAstNode;
