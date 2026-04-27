using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class TypeofExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is TypeofExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (TypeofExpressionNode)node;
        if (ctx.CurrentType?.GenericParameters.Any(p => p.Name == expression.TypeName) == true)
        {
            ctx.Writer.Write($"__genericArgs.{expression.TypeName}");
            return;
        }

        var name = ExpressionSymbolHelper.TryResolveType(ctx, expression.TypeName, out var type) ? NamingConvention.TypeName(type) : expression.TypeName;
        ctx.Writer.Write($"\"{ExpressionFormatHelper.Escape(name)}\"");
    }
}
