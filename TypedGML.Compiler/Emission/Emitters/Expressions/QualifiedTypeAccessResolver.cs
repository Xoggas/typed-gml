using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class QualifiedTypeAccessResolver
{
    public static bool TryResolveType(IAstNode node, EmitContext ctx, out TypeSymbol type)
    {
        if (TryResolve(node, ctx, out type, out var memberParts) && memberParts.Count == 0)
            return true;

        type = null!;
        return false;
    }

    public static bool TryResolveMember(
        MemberAccessExpressionNode access,
        EmitContext ctx,
        out TypeSymbol type,
        out string memberName)
    {
        if (TryResolve(access, ctx, out type, out var memberParts) && memberParts.Count == 1)
        {
            memberName = memberParts[0];
            return true;
        }

        type = null!;
        memberName = string.Empty;
        return false;
    }

    private static bool TryResolve(
        IAstNode node,
        EmitContext ctx,
        out TypeSymbol type,
        out IReadOnlyList<string> memberParts)
    {
        if (!TryGetParts(node, out var parts))
        {
            type = null!;
            memberParts = [];
            return false;
        }

        return QualifiedTypeNameResolver.TryResolve(
            parts,
            ctx.Symbols,
            CurrentNamespace(ctx),
            ctx.UsingPrefixes,
            out type,
            out memberParts);
    }

    private static bool TryGetParts(IAstNode node, out IReadOnlyList<string> parts)
    {
        switch (node)
        {
            case IdentifierExpressionNode identifier:
                parts = [identifier.Name];
                return true;
            case MemberAccessExpressionNode access when TryGetParts(access.Target, out var targetParts):
                parts = [.. targetParts, access.MemberName];
                return true;
            default:
                parts = [];
                return false;
        }
    }

    private static string CurrentNamespace(EmitContext ctx)
    {
        if (!string.IsNullOrEmpty(ctx.CurrentNamespacePrefix))
            return ctx.CurrentNamespacePrefix;
        if (ctx.CurrentType is null || !ctx.CurrentType.QualifiedName.Contains('.', StringComparison.Ordinal))
            return string.Empty;
        return ctx.CurrentType.QualifiedName[..ctx.CurrentType.QualifiedName.LastIndexOf('.')];
    }
}
