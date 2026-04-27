using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class MethodInvocationHelper
{
    public static bool TryRender(
        InvocationExpressionNode expression,
        EmitContext ctx,
        out string target,
        out string args)
    {
        if (TryResolveMethod(expression.Target, ctx, out var owner, out var member, out var receiver))
        {
            target = member.Modifiers.Contains("static", StringComparer.Ordinal)
                ? NamingConvention.StaticMemberName(owner, member)
                : NamingConvention.MethodName(owner, member);
            var ordered = ExpressionCallHelper.JoinArguments(expression.Target, expression.PositionalArgs, expression.NamedArgs, ctx);
            args = member.Modifiers.Contains("static", StringComparer.Ordinal) || string.IsNullOrEmpty(receiver)
                ? ordered
                : string.IsNullOrEmpty(ordered) ? receiver : $"{receiver}, {ordered}";
            return true;
        }

        target = string.Empty;
        args = string.Empty;
        return false;
    }

    private static bool TryResolveMethod(
        IAstNode target,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member,
        out string receiver)
    {
        switch (target)
        {
            case IdentifierExpressionNode identifier:
                receiver = ctx.SelfName ?? "self";
                return TryResolveCurrentMethod(identifier.Name, ctx, out owner, out member);
            case MemberAccessExpressionNode access when ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type):
                owner = type;
                member = type.Members.FirstOrDefault(m => m.Kind == MemberKind.Method && m.Name == access.MemberName)!;
                receiver = ctx.Emitter.Render(access.Target, ctx);
                return member is not null;
            default:
                owner = null!;
                member = null!;
                receiver = string.Empty;
                return false;
        }
    }

    private static bool TryResolveCurrentMethod(string name, EmitContext ctx, out TypeSymbol owner, out MemberSymbol member)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            member = current.Members.FirstOrDefault(m => m.Kind == MemberKind.Method && m.Name == name)!;
            if (member is null)
                continue;

            owner = current;
            return true;
        }

        owner = null!;
        member = null!;
        return false;
    }
}
