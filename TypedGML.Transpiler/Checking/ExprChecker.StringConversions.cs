using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool TryResolveIntrinsicStringConversion(string targetType, TgmlExpression expr, bool apply)
    {
        if (!string.Equals(targetType, "string", StringComparison.Ordinal))
            return false;

        if (EnumFacts.TryResolveMember(_ctx.TypeTable, expr, out var enumDecl, out var enumMember))
        {
            if (apply)
            {
                ClearResolvedConversion(expr);
                expr.Metadata[ObjectFacts.ResolvedStringLiteralConversionMetadata] = EnumFacts.GetQualifiedMemberName(enumDecl, enumMember);
            }

            return true;
        }

        var sourceType = InferType(expr);
        if (sourceType is not null && DelegateFacts.IsDelegateType(_ctx.TypeTable, sourceType))
        {
            if (apply)
            {
                ClearResolvedConversion(expr);
                expr.Metadata[ObjectFacts.ResolvedStringLiteralConversionMetadata] = "Function";
            }

            return true;
        }

        return false;
    }

    private static void ClearResolvedStringLiteralConversion(TgmlExpression expr)
        => expr.Metadata.Remove(ObjectFacts.ResolvedStringLiteralConversionMetadata);
}
