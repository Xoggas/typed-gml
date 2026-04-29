using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class NullConditionalInvocationHelper
{
    public static bool TryRender(InvocationExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (expression.Target is not NullConditionalExpressionNode conditional ||
            !ExpressionSymbolHelper.TryResolveTargetType(conditional.Target, ctx, out var owner))
            return false;

        var member = EmissionOverloadResolver.Pick(
            owner.Members.Where(m => m.Kind == MemberKind.Method && m.Name == conditional.MemberName).ToList(),
            expression.PositionalArgs,
            expression.NamedArgs,
            ctx);
        if (member is null)
            return false;

        var receiver = ctx.Emitter.Render(conditional.Target, ctx);
        var target = member.Modifiers.Contains("static", StringComparer.Ordinal)
            ? NamingConvention.StaticMemberName(owner, member)
            : NamingConvention.MethodName(owner, member);
        var orderedArgs = ExpressionCallHelper.JoinArguments(expression.Target, expression.PositionalArgs, expression.NamedArgs, ctx);
        var args = member.Modifiers.Contains("static", StringComparer.Ordinal)
            ? orderedArgs
            : string.IsNullOrEmpty(orderedArgs) ? receiver : $"{receiver}, {orderedArgs}";
        rendered = $"({receiver} != undefined ? {target}({args}) : undefined)";
        return true;
    }
}
