using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class MemberExpressionTypeLookup
{
    public static MemberSymbol? ResolveMember(IAstNode target, EmitContext ctx)
    {
        if (target is MemberAccessExpressionNode qualifiedAccess &&
            QualifiedTypeAccessResolver.TryResolveMember(qualifiedAccess, ctx, out var qualifiedOwner, out var qualifiedMember))
        {
            qualifiedOwner = PrimitiveBclTypeResolver.ResolveMemberOwner(qualifiedOwner, ctx.Symbols);
            return qualifiedOwner.Members.FirstOrDefault(m => m.Name == qualifiedMember);
        }

        if (target is MemberAccessExpressionNode access &&
            ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var owner))
            return GenericMemberResolver.FindMember(
                owner,
                ExpressionTypeLookup.Resolve(access.Target, ctx) ?? owner.QualifiedName,
                access.MemberName,
                out _);

        return null;
    }

    public static string? ResolveIndexer(IndexerAccessExpressionNode indexer, EmitContext ctx)
    {
        var targetTypeRef = ExpressionTypeLookup.Resolve(indexer.Target, ctx);
        if (string.IsNullOrWhiteSpace(targetTypeRef))
            return null;

        if (targetTypeRef.EndsWith("[]", StringComparison.Ordinal))
            return targetTypeRef[..^2];

        if (!ExpressionSymbolHelper.TryResolveType(ctx, targetTypeRef, out var targetType))
            return null;

        return GenericMemberResolver.Members(targetType, targetTypeRef, "this", MemberKind.Indexer)
            .FirstOrDefault()?.ReturnType;
    }
}
