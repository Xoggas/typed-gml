using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Statements;

public sealed record SwitchStatementNode(
    IAstNode Value,
    IReadOnlyList<SwitchSectionNode> Sections,
    SourceLocation Location) : IAstNode;
