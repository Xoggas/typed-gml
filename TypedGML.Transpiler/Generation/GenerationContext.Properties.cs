using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class GenerationContext
{
    public TgmlPropertyDecl? FindProperty(string name)
        => PropertyAccessHelper.FindPropertyInHierarchy(TypeTable, CurrentType, name)?.Property;

    public string? GetNativePropertyName(TgmlPropertyDecl? property)
    {
        if (property?.Metadata.TryGetValue("NativePropertyName", out var nativeName) == true &&
            nativeName is string s &&
            !string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        return null;
    }

    public string? GetNativePropertyName(string propertyName) => GetNativePropertyName(FindProperty(propertyName));

    public string? GetAssetName(TgmlMemberDecl? member)
        => AssetFacts.TryGetAssetName(member, out var assetName) ? assetName : null;

    public TgmlPropertyDecl? FindProperty(TgmlExpression target, string propertyName)
        => FindResolvedProperty(target, propertyName)?.Property;

    public PropertyAccessHelper.ResolvedProperty? FindResolvedProperty(TgmlExpression target, string propertyName)
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName ||
            !TryResolveType(typeName, out var decl) ||
            decl is null)
        {
            return null;
        }

        return PropertyAccessHelper.FindPropertyInHierarchy(TypeTable, decl, propertyName);
    }

    public TgmlPropertyDecl? FindIndexer(TgmlExpression target)
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName ||
            typeName.EndsWith("[]", StringComparison.Ordinal) ||
            !TryResolveType(typeName, out var decl) ||
            decl is null)
        {
            return null;
        }

        return PropertyAccessHelper.FindIndexerInHierarchy(TypeTable, decl)?.Property;
    }

    public bool TryResolveStaticAssetReference(TgmlExpression target, string memberName, out string emitted)
    {
        emitted = string.Empty;
        if (!TryGetQualifiedTypeName(target, out var qualifiedTypeName))
            return false;

        if (!TypeTable.TryResolve(qualifiedTypeName, out var decl) || decl is null)
            return false;

        return TryResolveStaticAssetReference(decl, memberName, out emitted, useQualifiedAccessor: true);
    }

    public bool TryResolveCurrentTypeAssetReference(string memberName, out string emitted)
    {
        emitted = string.Empty;
        return CurrentType is not null && TryResolveStaticAssetReference(CurrentType, memberName, out emitted, useQualifiedAccessor: false);
    }

    public bool IsInsideAccessorOf(string propertyName)
        => (InsideGetter || InsideSetter) && CurrentPropertyName == propertyName;

    private bool TryResolveStaticAssetReference(TgmlTypeDecl decl, string memberName, out string emitted, bool useQualifiedAccessor)
    {
        emitted = string.Empty;
        var qualifier = useQualifiedAccessor ? $"{GetGmlTypeName(decl)}." : string.Empty;

        switch (decl)
        {
            case TgmlClassDecl cls:
            {
                var property = cls.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (property is not null && AssetFacts.TryGetAssetName(property, out var propertyAssetName))
                {
                    emitted = $"{qualifier}get_{memberName}()";
                    return true;
                }

                var field = cls.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(field, out var fieldAssetName))
                {
                    emitted = fieldAssetName;
                    return true;
                }

                return false;
            }
            case TgmlStructDecl str:
            {
                var property = str.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (property is not null && AssetFacts.TryGetAssetName(property, out var propertyAssetName))
                {
                    emitted = $"{qualifier}get_{memberName}()";
                    return true;
                }

                var field = str.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(field, out var fieldAssetName))
                {
                    emitted = fieldAssetName;
                    return true;
                }

                return false;
            }
        }

        return false;
    }

    private bool TryResolveType(string describedType, out TgmlTypeDecl? decl)
    {
        var (baseName, typeArgs) = DelegateFacts.SplitDescribedType(describedType);
        while (baseName.EndsWith("[]", StringComparison.Ordinal))
            baseName = baseName[..^2];

        return TypeTable.TryResolve(MapPrimitiveType(baseName), typeArgs.Count, out decl);
    }

    private static string MapPrimitiveType(string typeName) => typeName switch
    {
        _ when BuiltinTypeFacts.TryGetBclTypeName(typeName, out var bclTypeName) => bclTypeName,
        _ when typeName.EndsWith("[]", StringComparison.Ordinal) => "System.Array",
        _ => typeName
    };

    private static string GetGmlTypeName(TgmlTypeDecl decl)
        => (decl.QualifiedName ?? decl.Name).Replace(".", "_");

    private bool TryGetQualifiedTypeName(TgmlExpression expression, out string qualifiedTypeName)
    {
        switch (expression)
        {
            case TgmlIdExpr id when !IsLocalShadow(id.Name) && !TryGetIdentifierAlias(id.Name, out _):
                qualifiedTypeName = id.Name;
                return true;

            case TgmlFieldAccessExpr fieldAccess when TryGetQualifiedTypeName(fieldAccess.Target, out var targetTypeName):
                qualifiedTypeName = targetTypeName + "." + fieldAccess.FieldName;
                return true;

            default:
                qualifiedTypeName = string.Empty;
                return false;
        }
    }
}
