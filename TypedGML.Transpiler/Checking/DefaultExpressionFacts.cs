using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static class DefaultExpressionFacts
{
    private const string ContextualDefaultTypeKey = "ContextualDefaultType";
    private const string ContextualDictionaryTypeKey = "ContextualDictionaryType";

    public static bool TryApplyContextualType(TgmlExpression expr, string targetType)
    {
        switch (expr)
        {
            case TgmlDictionaryInitExpr dict:
                dict.Metadata[ContextualDictionaryTypeKey] = targetType;
                return true;

            case TgmlDefaultExpr { Type: null } def:
                def.Metadata[ContextualDefaultTypeKey] = targetType;
                return true;

            case TgmlParenExpr p:
                return TryApplyContextualType(p.Inner, targetType);

            default:
                return false;
        }
    }

    public static string? GetEffectiveTypeName(TgmlDefaultExpr expr)
    {
        if (expr.Type is not null)
            return DescribeType(expr.Type);

        return expr.Metadata.TryGetValue(ContextualDefaultTypeKey, out var contextualType) &&
               contextualType is string targetType
            ? targetType
            : null;
    }

    public static string? GetEffectiveDictionaryTypeName(TgmlDictionaryInitExpr expr)
    {
        return expr.Metadata.TryGetValue(ContextualDictionaryTypeKey, out var contextualType) &&
               contextualType is string targetType
            ? targetType
            : null;
    }

    public static string DescribeType(TgmlTypeRef typeRef)
    {
        return typeRef.ArrayDepth > 0 || typeRef.TypeArgs.Count > 0
            ? typeRef.ToString()
            : BuiltinTypeFacts.Canonicalize(typeRef.Name.Full);
    }
}
