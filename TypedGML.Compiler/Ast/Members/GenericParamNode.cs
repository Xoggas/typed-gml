using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record GenericParamNode(
    string Name,
    string? Constraint,
    SourceLocation Location) : IAstNode;
