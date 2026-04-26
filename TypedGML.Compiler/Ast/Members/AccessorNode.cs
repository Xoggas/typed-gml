using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record AccessorNode(
    AccessorKind Kind,
    string? AccessMod,
    IAstNode? Body,
    SourceLocation Location) : IAstNode;
