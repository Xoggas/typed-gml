using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast;

public interface IAstNode
{
    SourceLocation Location { get; }
}
