using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class QualifiedTypeAccessResolver
{
    public static bool TryResolveType(IAstNode node, VerificationContext ctx, out TypeSymbol type)
    {
        if (TryResolve(node, ctx, out type, out var memberParts) && memberParts.Count == 0)
            return true;

        type = null!;
        return false;
    }

    public static bool TryResolveMember(
        MemberAccessExpressionNode access,
        VerificationContext ctx,
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

    public static bool IsNamespacePrefix(IAstNode node, VerificationContext ctx) =>
        TryGetParts(node, out var parts) &&
        QualifiedTypeNameResolver.IsNamespacePrefix(parts, ctx.Symbols);

    private static bool TryResolve(
        IAstNode node,
        VerificationContext ctx,
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
            TypeReferenceHelper.CurrentNamespace(ctx.CurrentType),
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
}
