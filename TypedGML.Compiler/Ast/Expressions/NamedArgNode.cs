using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record NamedArgNode(
    string Name,
    IAstNode Value,
    SourceLocation Location) : IAstNode;
