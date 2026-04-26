using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class DelegateEmitter : INodeEmitter
{
    private bool _helpersEmitted;

    public bool Matches(IAstNode node) => node is DelegateDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        if (_helpersEmitted)
            return;

        ctx.Writer.Write("function __tgml_invoke_delegate(_delegate)");
        ctx.Writer.BeginBlock();
        ctx.Writer.EndBlock();
        ctx.Writer.Write("function __tgml_delegate_remove(_delegate, _handler)");
        ctx.Writer.BeginBlock();
        ctx.Writer.WriteLine("return _delegate;");
        ctx.Writer.EndBlock();
        _helpersEmitted = true;
    }
}
