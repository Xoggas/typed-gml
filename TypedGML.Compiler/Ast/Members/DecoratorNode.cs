using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record DecoratorNode(
    string Name,
    IReadOnlyList<IAstNode> Args,
    SourceLocation Location) : IAstNode;
