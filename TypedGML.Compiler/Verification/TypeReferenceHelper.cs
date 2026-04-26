using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class TypeReferenceHelper
{
    public static bool IsNullable(string? typeRef) =>
        !string.IsNullOrEmpty(typeRef) && typeRef.EndsWith("?", StringComparison.Ordinal);

    public static string UnwrapNullable(string? typeRef) =>
        IsNullable(typeRef) ? typeRef![..^1] : typeRef ?? string.Empty;

    public static string RootName(string? typeRef)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return string.Empty;

        var stop = typeRef.IndexOfAny(['<', '?', '[']);
        return stop >= 0 ? typeRef[..stop] : typeRef;
    }

    public static string CurrentNamespace(TypeSymbol? currentType)
    {
        if (currentType is null)
            return string.Empty;

        var index = currentType.QualifiedName.LastIndexOf('.');
        return index < 0 ? string.Empty : currentType.QualifiedName[..index];
    }

    public static bool IsAssignable(string? targetType, string? sourceType, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(sourceType))
            return false;

        if (targetType == sourceType)
            return true;

        if (sourceType == "null")
            return IsNullable(targetType);

        if (IsNullable(targetType))
            return IsAssignable(UnwrapNullable(targetType), sourceType, ctx);

        if (IsNullable(sourceType))
            return false;

        if (targetType == "object" && sourceType != "void")
            return true;

        if (!SymbolResolver.TryResolveType(targetType, ctx, out var targetSymbol) ||
            !SymbolResolver.TryResolveType(sourceType, ctx, out var sourceSymbol))
            return false;

        if (targetSymbol == sourceSymbol)
            return true;

        for (var current = sourceSymbol.Base; current is not null; current = current.Base)
            if (current == targetSymbol)
                return true;

        if (sourceSymbol.Interfaces.Any(@interface => @interface == targetSymbol))
            return true;

        return HasConversion(sourceSymbol, sourceType, targetType, true) ||
               HasConversion(targetSymbol, sourceType, targetType, true);
    }

    public static bool HasConversion(TypeSymbol type, string sourceType, string targetType, bool implicitOnly) =>
        type.Members.Any(member =>
            member.Kind == MemberKind.ConversionOperator &&
            member.ReturnType == targetType &&
            member.Parameters.Count == 1 &&
            member.Parameters[0].TypeRef == sourceType &&
            (!implicitOnly || member.Name == ConversionOperatorKind.Implicit.ToString()));

    public static IReadOnlyList<string> TypeArguments(string? typeRef)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return [];

        var start = typeRef.IndexOf('<');
        var end = typeRef.LastIndexOf('>');
        if (start < 0 || end <= start)
            return [];

        return typeRef[(start + 1)..end]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool AreRelated(string? leftType, string? rightType, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(leftType) || string.IsNullOrWhiteSpace(rightType))
            return false;

        if (leftType == rightType ||
            IsAssignable(leftType, rightType, ctx) ||
            IsAssignable(rightType, leftType, ctx))
            return true;

        if (!SymbolResolver.TryResolveType(leftType, ctx, out var left) ||
            !SymbolResolver.TryResolveType(rightType, ctx, out var right))
            return false;

        return CommonAncestor(left, right);
    }

    private static bool CommonAncestor(TypeSymbol left, TypeSymbol right)
    {
        var seen = new HashSet<TypeSymbol>();
        for (var current = left; current is not null; current = current.Base)
            seen.Add(current);
        foreach (var @interface in left.Interfaces)
            seen.Add(@interface);

        for (var current = right; current is not null; current = current.Base)
            if (seen.Contains(current))
                return true;

        return right.Interfaces.Any(seen.Contains);
    }
}
