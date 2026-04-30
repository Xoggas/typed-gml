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
        if (TryResolveMethod(expression, ctx, out var owner, out var member, out var receiver))
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
        InvocationExpressionNode expression,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member,
        out string receiver)
    {
        switch (expression.Target)
        {
            case IdentifierExpressionNode identifier:
                receiver = ctx.SelfName ?? "self";
                return TryResolveCurrentMethod(identifier.Name, expression, ctx, out owner, out member);
            case MemberAccessExpressionNode access when QualifiedTypeAccessResolver.TryResolveMember(access, ctx, out var qualifiedType, out var qualifiedMember):
                qualifiedType = PrimitiveBclTypeResolver.ResolveMemberOwner(qualifiedType, ctx.Symbols);
                owner = qualifiedType;
                member = EmissionOverloadResolver.Pick(
                    qualifiedType.Members.Where(m => m.Kind == MemberKind.Method && m.Name == qualifiedMember).ToList(),
                    expression.PositionalArgs,
                    expression.NamedArgs,
                    ctx)!;
                receiver = string.Empty;
                return member is not null;
            case MemberAccessExpressionNode access when ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type):
                owner = type;
                member = EmissionOverloadResolver.Pick(
                    type.Members.Where(m => m.Kind == MemberKind.Method && m.Name == access.MemberName).ToList(),
                    expression.PositionalArgs,
                    expression.NamedArgs,
                    ctx)!;
                receiver = ctx.Emitter.Render(access.Target, ctx);
                return member is not null;
            default:
                owner = null!;
                member = null!;
                receiver = string.Empty;
                return false;
        }
    }

    private static bool TryResolveCurrentMethod(
        string name,
        InvocationExpressionNode expression,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            member = EmissionOverloadResolver.Pick(
                current.Members.Where(m => m.Kind == MemberKind.Method && m.Name == name).ToList(),
                expression.PositionalArgs,
                expression.NamedArgs,
                ctx)!;
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
