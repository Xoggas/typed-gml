using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record DictionaryLiteralExpressionNode(
    IReadOnlyList<DictionaryEntryNode> Entries,
    SourceLocation Location) : IAstNode;
