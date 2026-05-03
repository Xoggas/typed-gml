using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record VarDeclarationStatementNode(
    string Name,
    string? TypeRef,
    IAstNode? Initializer,
    bool IsVar,
    SourceLocation Location) : IAstNode;
