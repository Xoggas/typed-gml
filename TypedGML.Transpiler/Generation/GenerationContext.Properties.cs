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
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName ||
            !TryResolveType(typeName, out var decl) ||
            decl is null)
        {
            return null;
        }

        return PropertyAccessHelper.FindPropertyInHierarchy(TypeTable, decl, propertyName)?.Property;
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

    public bool TryResolveStaticAssetReference(TgmlExpression target, string memberName, out string assetName)
    {
        assetName = string.Empty;
        if (target is not TgmlIdExpr id)
            return false;

        if (!TypeTable.TryResolve(id.Name, out var decl) || decl is null)
            return false;

        return TryResolveStaticAssetReference(decl, memberName, out assetName);
    }

    public bool TryResolveCurrentTypeAssetReference(string memberName, out string assetName)
    {
        assetName = string.Empty;
        return CurrentType is not null && TryResolveStaticAssetReference(CurrentType, memberName, out assetName);
    }

    public bool IsInsideAccessorOf(string propertyName)
        => (InsideGetter || InsideSetter) && CurrentPropertyName == propertyName;

    private bool TryResolveStaticAssetReference(TgmlTypeDecl decl, string memberName, out string assetName)
    {
        assetName = string.Empty;

        switch (decl)
        {
            case TgmlClassDecl cls:
            {
                var property = cls.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(property, out assetName))
                    return true;

                var field = cls.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                return AssetFacts.TryGetAssetName(field, out assetName);
            }
            case TgmlStructDecl str:
            {
                var property = str.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(property, out assetName))
                    return true;

                var field = str.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                return AssetFacts.TryGetAssetName(field, out assetName);
            }
        }

        return false;
    }

    private bool TryResolveType(string describedType, out TgmlTypeDecl? decl)
    {
        var (baseName, typeArgs) = DelegateFacts.SplitDescribedType(describedType);
        while (baseName.EndsWith("[]", StringComparison.Ordinal))
            baseName = baseName[..^2];

        return TypeTable.TryResolve(baseName, typeArgs.Count, out decl);
    }
}
