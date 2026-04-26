using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record ParameterNode(
    string Name,
    string TypeRef,
    IAstNode? DefaultValue,
    IReadOnlyList<DecoratorNode> Decorators,
    SourceLocation Location) : IAstNode;
