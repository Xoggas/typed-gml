using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record DictionaryEntryNode(
    IAstNode Key,
    IAstNode Value,
    SourceLocation Location) : IAstNode;
