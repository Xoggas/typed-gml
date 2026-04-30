using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class StaticMemberAccessHelper
{
    public static bool TryRenderRead(IAstNode node, EmitContext ctx, out string rendered)
    {
        if (!TryResolve(node, ctx, out var owner, out var member))
        {
            rendered = string.Empty;
            return false;
        }

        var isStatic = member.Modifiers.Contains("static", StringComparer.Ordinal);
        var isConst = member.Modifiers.Contains("const", StringComparer.Ordinal);
        if (!isStatic && !isConst)
        {
            rendered = string.Empty;
            return false;
        }

        rendered = member.Kind switch
        {
            MemberKind.Method => NamingConvention.StaticMemberName(owner, member),
            MemberKind.Field when isConst => NamingConvention.ConstMacro(owner, member),
            MemberKind.Field => NamingConvention.StaticMemberName(owner, member),
            MemberKind.Property => $"{NamingConvention.StaticGetterName(owner, member)}()",
            _ => string.Empty
        };
        return !string.IsNullOrEmpty(rendered);
    }

    public static bool TryRenderAssignment(AssignmentExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!TryResolve(expression.Target, ctx, out var owner, out var member) || !member.Modifiers.Contains("static", StringComparer.Ordinal))
            return false;

        var value = ctx.Emitter.Render(expression.Value, ctx);
        if (member.Kind == MemberKind.Field)
        {
            rendered = $"{NamingConvention.StaticMemberName(owner, member)} {expression.Op} {value}";
            return true;
        }

        if (member.Kind != MemberKind.Property)
            return false;

        if (expression.Op == "=")
        {
            rendered = $"{NamingConvention.StaticSetterName(owner, member)}({value})";
            return true;
        }

        if (!expression.Op.EndsWith("=", StringComparison.Ordinal) || expression.Op.Length == 1)
            return false;

        var op = expression.Op[..^1];
        rendered = $"{NamingConvention.StaticSetterName(owner, member)}({NamingConvention.StaticGetterName(owner, member)}() {op} {value})";
        return true;
    }

    private static bool TryResolve(IAstNode node, EmitContext ctx, out TypeSymbol owner, out MemberSymbol member)
    {
        switch (node)
        {
            case MemberAccessExpressionNode access when QualifiedTypeAccessResolver.TryResolveMember(access, ctx, out var qualifiedType, out var qualifiedMember):
                qualifiedType = PrimitiveBclTypeResolver.ResolveMemberOwner(qualifiedType, ctx.Symbols);
                return TryResolveMember(qualifiedType, qualifiedMember, out owner, out member);
            case MemberAccessExpressionNode access when TryResolveType(access.Target, ctx, out var type):
                return TryResolveMember(type, access.MemberName, out owner, out member);
            case IdentifierExpressionNode identifier when ctx.CurrentType is not null:
                if (TryResolveMember(ctx.CurrentType, identifier.Name, out owner, out member))
                    return true;
                owner = null!;
                member = null!;
                return false;
            default:
                owner = null!;
                member = null!;
                return false;
        }
    }

    private static bool TryResolveType(IAstNode target, EmitContext ctx, out TypeSymbol type)
    {
        if (ExpressionSymbolHelper.TryResolveTargetType(target, ctx, out type))
            return true;

        if (target is IdentifierExpressionNode identifier)
            return TryResolveBclAlias(identifier.Name, ctx, out type);

        type = null!;
        return false;
    }

    private static bool TryResolveBclAlias(string name, EmitContext ctx, out TypeSymbol type)
    {
        foreach (var alias in Aliases(name))
            if (ctx.Symbols.TryResolve(alias, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out type))
                return true;

        type = null!;
        return false;
    }

    private static IReadOnlyList<string> Aliases(string name) => name switch
    {
        "Number" => ["Math", "number"],
        "String" => ["string"],
        "Bool" => ["bool"],
        _ => []
    };

    private static bool TryResolveMember(TypeSymbol? type, string name, out TypeSymbol owner, out MemberSymbol member)
    {
        for (var current = type; current is not null; current = current.Base)
        {
            member = current.Members.FirstOrDefault(m => m.Name == name)!;
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
