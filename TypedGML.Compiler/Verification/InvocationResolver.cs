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
            return MemberSignatureHelper.Members(owner, memberName, MemberKind.Method).ToList();

        if (!SymbolResolver.TryResolveType(ExpressionTypeResolver.Resolve(access.Target, ctx), ctx, out var targetType))
            return [];

        var methods = MemberSignatureHelper.Members(targetType, access.MemberName, MemberKind.Method).ToList();
        if (methods.Count > 0)
            return methods;

        var member = SymbolResolver.FindMember(targetType, access.MemberName, out _);
        delegateInvocation = IsDelegateType(member?.ReturnType, ctx);
        return [];
    }

    private static IReadOnlyList<MemberSymbol> ResolveNullConditional(
        NullConditionalExpressionNode conditional,
        VerificationContext ctx,
        out bool delegateInvocation)
    {
        delegateInvocation = false;
        if (!SymbolResolver.TryResolveType(ExpressionTypeResolver.Resolve(conditional.Target, ctx), ctx, out var targetType))
            return [];

        return MemberSignatureHelper.Members(targetType, conditional.MemberName, MemberKind.Method).ToList();
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

        var methods = MemberSignatureHelper.Members(ctx.CurrentType, identifier.Name, MemberKind.Method).ToList();
        if (methods.Count > 0)
            return methods;

        delegateInvocation = IsDelegateType(identifier.Name, ctx);
        return [];
    }

    private static bool IsDelegateType(string? typeRef, VerificationContext ctx) =>
        SymbolResolver.TryResolveType(typeRef, ctx, out var symbol) && symbol.Kind == TypeKind.Delegate;
}
