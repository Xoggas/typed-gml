using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

internal static class ArrayLiteralTargetHelper
{
    public static bool IsLiteralTarget(string? typeRef, VerificationContext ctx) =>
        TargetElementType(typeRef, ctx) is not null;

    public static string? TargetElementType(string? typeRef, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return null;

        if (typeRef.EndsWith("[]", StringComparison.Ordinal))
            return typeRef[..^2];

        if (!IsListTarget(typeRef, ctx))
            return null;

        return TypeReferenceHelper.TypeArguments(typeRef).FirstOrDefault();
    }

    public static bool IsListTarget(string? typeRef, VerificationContext ctx) =>
        !string.IsNullOrWhiteSpace(typeRef) &&
        SymbolResolver.TryResolveType(typeRef, ctx, out var type) &&
        type.Arity == 1 &&
        ShortName(type) == "List";

    private static string ShortName(TypeSymbol type)
    {
        var index = type.QualifiedName.LastIndexOf('.');
        return index < 0 ? type.QualifiedName : type.QualifiedName[(index + 1)..];
    }
}
