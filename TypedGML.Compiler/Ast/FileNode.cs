using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast;

public sealed record FileNode(
    string FilePath,
    IReadOnlyList<IAstNode> TopLevelDeclarations,
    SourceLocation Location) : IAstNode;
