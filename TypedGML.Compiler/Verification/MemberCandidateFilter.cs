using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class MemberCandidateFilter
{
    public static IEnumerable<MemberSymbol> MostDerived(IEnumerable<MemberSymbol> members)
    {
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members)
            if (signatures.Add(SignatureKey(member)))
                yield return member;
    }

    private static string SignatureKey(MemberSymbol member) =>
        $"{member.Name}({string.Join(",", member.Parameters.Select(parameter => parameter.TypeRef))})";
}
