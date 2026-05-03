using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class NullConditionalTypeResolver
{
    public static string? ResolveAccess(NullConditionalExpressionNode conditional, VerificationContext ctx)
    {
        if (!TryResolveTarget(conditional, ctx, out var targetType, out var targetTypeRef))
            return null;

        var member = GenericMemberResolver.FindMember(targetType, targetTypeRef, conditional.MemberName, out _);
        return Nullable(member?.ReturnType);
    }

    public static string? ResolveInvocation(
        NullConditionalExpressionNode conditional,
        InvocationExpressionNode invocation,
        VerificationContext ctx)
    {
        if (!TryResolveTarget(conditional, ctx, out var targetType, out var targetTypeRef))
            return null;

        var matches = GenericMemberResolver.Members(targetType, targetTypeRef, conditional.MemberName, MemberKind.Method)
            .Where(candidate => Matches(candidate, invocation, ctx))
            .Take(2)
            .ToList();

        return matches.Count == 1 ? Nullable(matches[0].ReturnType) : null;
    }

    private static bool TryResolveTarget(
        NullConditionalExpressionNode conditional,
        VerificationContext ctx,
        out TypeSymbol targetType,
        out string? targetTypeRef)
    {
        targetTypeRef = ExpressionTypeResolver.Resolve(conditional.Target, ctx);
        return SymbolResolver.TryResolveType(targetTypeRef, ctx, out targetType);
    }

    private static bool Matches(MemberSymbol candidate, InvocationExpressionNode invocation, VerificationContext ctx)
    {
        if (invocation.PositionalArgs.Count > candidate.Parameters.Count)
            return false;

        var suppliedCount = invocation.PositionalArgs.Count + invocation.NamedArgs.Count;
        if (suppliedCount < MemberSignatureHelper.RequiredParameters(candidate) || suppliedCount > candidate.Parameters.Count)
            return false;

        for (var i = 0; i < invocation.PositionalArgs.Count; i++)
            if (!TypeReferenceHelper.IsAssignable(candidate.Parameters[i].TypeRef, ExpressionTypeResolver.Resolve(invocation.PositionalArgs[i], ctx), ctx))
                return false;

        foreach (var namedArg in invocation.NamedArgs)
        {
            var index = IndexOf(candidate, namedArg.Name);
            if (index < 0 || index < invocation.PositionalArgs.Count)
                return false;

            if (!TypeReferenceHelper.IsAssignable(candidate.Parameters[index].TypeRef, ExpressionTypeResolver.Resolve(namedArg.Value, ctx), ctx))
                return false;
        }

        return true;
    }

    private static int IndexOf(MemberSymbol candidate, string name)
    {
        for (var i = 0; i < candidate.Parameters.Count; i++)
            if (candidate.Parameters[i].Name == name)
                return i;

        return -1;
    }

    private static string? Nullable(string? typeRef) =>
        string.IsNullOrWhiteSpace(typeRef) || typeRef == "void"
            ? typeRef
            : TypeReferenceHelper.IsNullable(typeRef) ? typeRef : $"{typeRef}?";
}
