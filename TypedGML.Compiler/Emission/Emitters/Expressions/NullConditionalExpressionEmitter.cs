using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class NullConditionalExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is NullConditionalExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (NullConditionalExpressionNode)node;
        var target = ctx.Emitter.Render(expression.Target, ctx);
        if (ctx.CanEmitTempPrelude && !IsSimple(expression.Target))
        {
            var temp = ctx.NextTempVarName();
            ctx.AddTempPreludeLine($"var {temp} = {target};");
            target = temp;
        }

        ctx.Writer.Write($"({target} != undefined ? {RenderMemberRead(expression, target, ctx)} : undefined)");
    }

    private static bool IsSimple(IAstNode node) =>
        node is IdentifierExpressionNode or LiteralExpressionNode;

    private static string RenderMemberRead(NullConditionalExpressionNode expression, string target, EmitContext ctx)
    {
        if (!ExpressionSymbolHelper.TryResolveTargetType(expression.Target, ctx, out var owner))
            return $"{target}.{expression.MemberName}";

        var member = owner.Members.FirstOrDefault(m => m.Name == expression.MemberName);
        return member?.Kind switch
        {
            MemberKind.Property when member.NativePropertyName is not null => $"{target}.{member.NativePropertyName}",
            MemberKind.Property => $"{NamingConvention.PropertyGetter(owner, member)}({target})",
            _ => $"{target}.{expression.MemberName}"
        };
    }
}
