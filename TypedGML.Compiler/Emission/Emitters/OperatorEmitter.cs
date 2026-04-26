using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class OperatorEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is OperatorDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var operatorNode = (OperatorDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var parameters = string.Join(", ", operatorNode.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function {NamingConvention.OperatorName(ctx.CurrentType, operatorNode.OperatorSymbol)}({parameters})");
        ctx.Writer.BeginBlock();
        if (operatorNode.Body is not null)
            ctx.Dispatch(operatorNode.Body, ctx);
        ctx.Writer.EndBlock();
    }
}
