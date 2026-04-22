using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private string? InferLValueType(TgmlExpression target)
    {
        return target switch
        {
            TgmlIdExpr id => InferLValueIdType(id),
            TgmlFieldAccessExpr fieldAccess => InferLValueFieldAccessType(fieldAccess),
            TgmlIndexExpr indexExpr => VisitIndex(indexExpr),
            _ => InferType(target)
        };
    }

    private string? InferLValueIdType(TgmlIdExpr id)
    {
        if (Symbols.TryResolve(id.Name, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);

        var prop = ResolvePropertyOnCurrentType(id.Name);
        if (prop is not null)
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);

        var (field, fieldDeclType2) = FindFieldWithDecl(_owner, id.Name);
        if (field is not null)
        {
            if (fieldDeclType2 is not null &&
                !PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, fieldDeclType2, field.Access))
            {
                Error(id, $"Field '{id.Name}' is inaccessible due to its protection level.");
                return null;
            }
            return DefaultExpressionFacts.DescribeType(field.Type);
        }
        return null;
    }

    private string? InferLValueFieldAccessType(TgmlFieldAccessExpr expr)
    {
        if (TryResolveStaticAssetMemberAccess(expr, out var assetName, out var assetType))
        {
            expr.Metadata[AssetReferenceNameMetadata] = assetName;
            return assetType;
        }

        var targetType = InferType(expr.Target);
        if (targetType is null) return null;

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        var prop = PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, expr.FieldName);
        if (prop is not null)
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);

        var (field, fieldDeclaringType) = FindFieldWithDecl(targetDecl, expr.FieldName);
        if (field is null)
        {
            Error(expr, $"Type '{targetType}' does not contain a member '{expr.FieldName}'.");
            return null;
        }

        if (!PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, fieldDeclaringType!, field.Access))
            Error(expr, $"Field '{expr.FieldName}' is inaccessible due to its protection level.");

        return DefaultExpressionFacts.DescribeType(field.Type);
    }

    private bool TryResolveAssetAccess(TgmlExpression target, out string assetName, out string assetType)
    {
        switch (target)
        {
            case TgmlIdExpr id when TryResolveCurrentTypeAssetMember(id.Name, out assetName, out assetType):
                id.Metadata[AssetReferenceNameMetadata] = assetName;
                return true;

            case TgmlFieldAccessExpr fieldAccess when TryResolveStaticAssetMemberAccess(fieldAccess, out assetName, out assetType):
                fieldAccess.Metadata[AssetReferenceNameMetadata] = assetName;
                return true;

            default:
                assetName = string.Empty;
                assetType = string.Empty;
                return false;
        }
    }

    private bool TryResolveCurrentTypeAssetMember(string memberName, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;
        if (_owner is null)
            return false;

        return TryResolveAssetMember(_owner, memberName, staticOnly: true, out assetName, out assetType);
    }

    private bool TryResolveStaticAssetMemberAccess(TgmlFieldAccessExpr access, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;

        if (access.Target is not TgmlIdExpr id)
            return false;

        if (Symbols.TryResolve(id.Name, out var symbolType) && symbolType is not null)
            return false;

        if (!_ctx.TypeTable.TryResolve(id.Name, out var targetDecl) || targetDecl is null)
            return false;

        return TryResolveAssetMember(targetDecl, access.FieldName, staticOnly: true, out assetName, out assetType);
    }

    private static bool TryResolveAssetMember(TgmlTypeDecl owner, string memberName, bool staticOnly, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;

        switch (owner)
        {
            case TgmlClassDecl cls:
            {
                var property = cls.Properties.FirstOrDefault(p =>
                    (!staticOnly || p.IsStatic) &&
                    string.Equals(p.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(p, out _));
                if (property is not null && AssetFacts.TryGetAssetName(property, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(property.Type);
                    return true;
                }

                var field = cls.Fields.FirstOrDefault(f =>
                    (!staticOnly || f.IsStatic) &&
                    string.Equals(f.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(f, out _));
                if (field is not null && AssetFacts.TryGetAssetName(field, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(field.Type);
                    return true;
                }

                break;
            }

            case TgmlStructDecl str:
            {
                var property = str.Properties.FirstOrDefault(p =>
                    (!staticOnly || p.IsStatic) &&
                    string.Equals(p.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(p, out _));
                if (property is not null && AssetFacts.TryGetAssetName(property, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(property.Type);
                    return true;
                }

                var field = str.Fields.FirstOrDefault(f =>
                    (!staticOnly || f.IsStatic) &&
                    string.Equals(f.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(f, out _));
                if (field is not null && AssetFacts.TryGetAssetName(field, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(field.Type);
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private static bool TryGetArrayElementType(string describedType, out string elementType)
    {
        if (describedType.EndsWith("[]", StringComparison.Ordinal))
        {
            elementType = describedType[..^2];
            return true;
        }

        elementType = string.Empty;
        return false;
    }

}
