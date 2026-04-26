using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class EventEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is EventDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
    }
}
