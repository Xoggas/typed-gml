using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class SymbolResolver
{
    public static bool TryResolveType(string? typeRef, VerificationContext ctx, out TypeSymbol symbol) =>
        ctx.Symbols.TryResolve(
            typeRef ?? string.Empty,
            TypeReferenceHelper.CurrentNamespace(ctx.CurrentType),
            ctx.UsingPrefixes,
            out symbol);

    public static bool TryResolveType(string typeName, int arity, VerificationContext ctx, out TypeSymbol symbol) =>
        ctx.Symbols.TryResolve(
            typeName,
            arity,
            TypeReferenceHelper.CurrentNamespace(ctx.CurrentType),
            ctx.UsingPrefixes,
            out symbol);

    public static MemberSymbol? FindMember(TypeSymbol? type, string name, out TypeSymbol? owner)
    {
        for (var current = type; current is not null; current = current.Base)
        {
            var member = current.Members.FirstOrDefault(m => m.Name == name);
            if (member is not null)
            {
                owner = current;
                return member;
            }
        }

        owner = null;
        return null;
    }

    public static bool IsWithinInheritance(TypeSymbol? currentType, TypeSymbol owner)
    {
        for (var current = currentType; current is not null; current = current.Base)
            if (current == owner)
                return true;

        return false;
    }
}
