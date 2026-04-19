using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Shared facts about compile-time asset references exposed through <c>@Asset("name")</c>.
/// </summary>
public static class AssetFacts
{
    public const string DecoratorName = "Asset";
    public const string AssetNameMetadata = "AssetName";
    public const string AssetReferenceNameMetadata = "AssetReferenceName";

    private static readonly HashSet<string> SupportedAssetTypes =
    [
        "Sprite",
        "Room",
        "Audio",
        "Font"
    ];

    public static bool IsAssetType(TgmlTypeRef type)
        => IsAssetType(DefaultExpressionFacts.DescribeType(type));

    public static bool IsAssetType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        var baseName = typeName;
        var genericStart = baseName.IndexOf('<');
        if (genericStart >= 0)
            baseName = baseName[..genericStart];

        while (baseName.EndsWith("[]", StringComparison.Ordinal))
            baseName = baseName[..^2];

        var lastDot = baseName.LastIndexOf('.');
        if (lastDot >= 0)
            baseName = baseName[(lastDot + 1)..];

        return SupportedAssetTypes.Contains(baseName);
    }

    public static bool TryGetAssetName(TgmlMemberDecl? member, out string assetName)
    {
        assetName = string.Empty;
        if (member is null)
            return false;

        if (member.Metadata.TryGetValue(AssetNameMetadata, out var metadataValue) &&
            metadataValue is string metadataName &&
            !string.IsNullOrWhiteSpace(metadataName))
        {
            assetName = metadataName;
            return true;
        }

        var decoratorValue = member.GetDecorator(DecoratorName)?.GetFirstStringArg();
        if (string.IsNullOrWhiteSpace(decoratorValue))
            return false;

        assetName = decoratorValue!;
        return true;
    }
}
