using TypedGML.Compiler.Ast;

namespace TypedGML.Compiler.Emission;

public interface INodeEmitter
{
    bool Matches(IAstNode node);

    void Emit(IAstNode node, EmitContext ctx);
}
