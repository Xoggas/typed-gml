using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class GenericMemberResolver
{
    public static MemberSymbol? FindMember(
        TypeSymbol? type,
        string? receiverTypeRef,
        string name,
        out TypeSymbol? owner)
    {
        for (var current = type; current is not null; current = current.Base)
        {
            var substituted = ApplySubstitution(current, receiverTypeRef);
            var member = substituted.Members.FirstOrDefault(candidate => candidate.Name == name);
            if (member is not null)
            {
                owner = current;
                return member;
            }
        }

        owner = null;
        return null;
    }

    public static IEnumerable<MemberSymbol> Members(
        TypeSymbol? type,
        string? receiverTypeRef,
        string name,
        MemberKind? kind = null)
    {
        for (var current = type; current is not null; current = current.Base)
        {
            var substituted = ApplySubstitution(current, receiverTypeRef);
            foreach (var member in substituted.Members)
                if (member.Name == name && (kind is null || member.Kind == kind))
                    yield return member;
        }
    }

    private static TypeSymbol ApplySubstitution(TypeSymbol type, string? receiverTypeRef) =>
        GenericTypeSubstitution.ApplySubstitution(type, GenericTypeSubstitution.Map(type, receiverTypeRef));
}
