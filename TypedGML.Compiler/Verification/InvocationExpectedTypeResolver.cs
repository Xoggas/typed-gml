using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class InvocationExpectedTypeResolver
{
    public static string? Positional(InvocationExpressionNode invocation, int index, VerificationContext ctx) =>
        PickCandidate(invocation, ctx) is { } candidate && index < candidate.Parameters.Count
            ? candidate.Parameters[index].TypeRef
            : null;

    public static string? Named(InvocationExpressionNode invocation, string name, VerificationContext ctx)
    {
        var candidate = PickCandidate(invocation, ctx);
        return candidate?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.TypeRef;
    }

    private static MemberSymbol? PickCandidate(InvocationExpressionNode invocation, VerificationContext ctx)
    {
        var candidates = InvocationResolver.ResolveCandidates(invocation, ctx, out _)
            .Where(candidate => ArgumentShapeMatches(candidate, invocation))
            .ToList();
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static bool ArgumentShapeMatches(MemberSymbol candidate, InvocationExpressionNode invocation)
    {
        var suppliedCount = invocation.PositionalArgs.Count + invocation.NamedArgs.Count;
        return invocation.PositionalArgs.Count <= candidate.Parameters.Count &&
            suppliedCount >= MemberSignatureHelper.RequiredParameters(candidate) &&
            suppliedCount <= candidate.Parameters.Count &&
            invocation.NamedArgs.All(arg => candidate.Parameters.Any(parameter => parameter.Name == arg.Name));
    }
}
