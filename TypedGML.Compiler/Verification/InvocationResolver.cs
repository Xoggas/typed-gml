using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class InvocationResolver
{
    public static IReadOnlyList<MemberSymbol> ResolveCandidates(
        InvocationExpressionNode invocation,
        VerificationContext ctx,
        out bool delegateInvocation)
    {
        delegateInvocation = false;
        if (invocation.Target is MemberAccessExpressionNode access)
            return ResolveMemberAccess(access, ctx, out delegateInvocation);

        if (invocation.Target is NullConditionalExpressionNode conditional)
            return ResolveNullConditional(conditional, ctx, out delegateInvocation);

        if (invocation.Target is IdentifierExpressionNode identifier)
            return ResolveIdentifier(identifier, ctx, out delegateInvocation);

        var targetType = ExpressionTypeResolver.Resolve(invocation.Target, ctx);
        delegateInvocation = IsDelegateType(targetType, ctx);
        return [];
    }

    private static IReadOnlyList<MemberSymbol> ResolveMemberAccess(
        MemberAccessExpressionNode access,
        VerificationContext ctx,
        out bool delegateInvocation)
    {
        delegateInvocation = false;
        if (QualifiedTypeAccessResolver.TryResolveMember(access, ctx, out var owner, out var memberName))
        {
            owner = PrimitiveBclTypeResolver.ResolveMemberOwner(owner, ctx.Symbols);
            var qualifiedMethods = MemberCandidateFilter.MostDerived(MemberSignatureHelper.Members(owner, memberName, MemberKind.Method)).ToList();
            if (qualifiedMethods.Count > 0)
                return qualifiedMethods;

            var qualifiedMember = SymbolResolver.FindMember(owner, memberName, out _);
            delegateInvocation = IsDelegateLike(qualifiedMember, ctx);
            return [];
        }

        var targetTypeRef = ExpressionTypeResolver.Resolve(access.Target, ctx);
        if (!SymbolResolver.TryResolveType(targetTypeRef, ctx, out var targetType))
            return [];

        var methods = MemberCandidateFilter.MostDerived(
            GenericMemberResolver.Members(targetType, targetTypeRef, access.MemberName, MemberKind.Method)).ToList();
        if (methods.Count > 0)
            return methods;

        var member = GenericMemberResolver.FindMember(targetType, targetTypeRef, access.MemberName, out _);
        delegateInvocation = IsDelegateLike(member, ctx);
        return [];
    }

    private static IReadOnlyList<MemberSymbol> ResolveNullConditional(
        NullConditionalExpressionNode conditional,
        VerificationContext ctx,
        out bool delegateInvocation)
    {
        delegateInvocation = false;
        var targetTypeRef = ExpressionTypeResolver.Resolve(conditional.Target, ctx);
        if (!SymbolResolver.TryResolveType(targetTypeRef, ctx, out var targetType))
            return [];

        return MemberCandidateFilter.MostDerived(
            GenericMemberResolver.Members(targetType, targetTypeRef, conditional.MemberName, MemberKind.Method)).ToList();
    }

    private static IReadOnlyList<MemberSymbol> ResolveIdentifier(
        IdentifierExpressionNode identifier,
        VerificationContext ctx,
        out bool delegateInvocation)
    {
        delegateInvocation = false;
        if (ctx.Scope.TryResolve(identifier.Name, out var typeRef))
        {
            delegateInvocation = IsDelegateType(typeRef, ctx);
            return [];
        }

        var methods = MemberCandidateFilter.MostDerived(
            MemberSignatureHelper.Members(ctx.CurrentType, identifier.Name, MemberKind.Method)).ToList();
        if (methods.Count > 0)
            return methods;

        var member = SymbolResolver.FindMember(ctx.CurrentType, identifier.Name, out _);
        delegateInvocation = IsDelegateLike(member, ctx) || IsDelegateType(identifier.Name, ctx);
        return [];
    }

    private static bool IsDelegateLike(MemberSymbol? member, VerificationContext ctx) =>
        member is not null &&
        (member.Kind == MemberKind.Event || IsDelegateType(member.ReturnType, ctx));

    private static bool IsDelegateType(string? typeRef, VerificationContext ctx) =>
        SymbolResolver.TryResolveType(typeRef, ctx, out var symbol) && symbol.Kind == TypeKind.Delegate;
}
