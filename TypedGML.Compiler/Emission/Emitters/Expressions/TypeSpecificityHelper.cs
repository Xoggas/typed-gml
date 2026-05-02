using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class TypeSpecificityHelper
{
    public static string MostSpecific(string? leftTypeRef, string? rightTypeRef, EmitContext ctx)
    {
        if (string.IsNullOrWhiteSpace(leftTypeRef))
            return rightTypeRef ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rightTypeRef) || leftTypeRef == rightTypeRef)
            return leftTypeRef;

        if (!ExpressionSymbolHelper.TryResolveType(ctx, leftTypeRef, out var left) ||
            !ExpressionSymbolHelper.TryResolveType(ctx, rightTypeRef, out var right))
            return leftTypeRef;

        if (DerivesFrom(right, left))
            return rightTypeRef;

        return DerivesFrom(left, right) ? leftTypeRef : leftTypeRef;
    }

    private static bool DerivesFrom(TypeSymbol source, TypeSymbol target)
    {
        for (var current = source; current is not null; current = current.Base)
            if (current == target)
                return true;

        return source.Interfaces.Any(@interface => @interface == target);
    }
}
