using System.Text;

namespace TypedGML.CLI.GameMaker;

internal static class AssetRegistryGenerator
{
    private sealed record AssetRegistrySpec(string FileName, string ClassName, string AssetDirectoryName, string TypeName);

    private static readonly AssetRegistrySpec[] Specs =
    [
        new("Sprites.tgml", "Sprites", "sprites", "System.Sprite"),
        new("Sounds.tgml", "Sounds", "sounds", "System.Audio"),
        new("Rooms.tgml", "Rooms", "rooms", "System.Room"),
        new("Fonts.tgml", "Fonts", "fonts", "System.Font")
    ];

    public static void Generate(string projectRootPath, string typedGmlSourcePath)
    {
        var assetDirectoryPath = Path.Combine(typedGmlSourcePath, "Assets");
        Directory.CreateDirectory(assetDirectoryPath);

        foreach (var spec in Specs)
        {
            var assets = DiscoverAssets(projectRootPath, spec.AssetDirectoryName);
            var content = BuildRegistryContent(spec, assets);
            File.WriteAllText(Path.Combine(assetDirectoryPath, spec.FileName), content);
        }
    }

    private static IReadOnlyList<string> DiscoverAssets(string projectRootPath, string assetDirectoryName)
    {
        var root = Path.Combine(projectRootPath, assetDirectoryName);
        if (Directory.Exists(root) is false)
            return [];

        return Directory
            .EnumerateFiles(root, "*.yy", SearchOption.AllDirectories)
            .Where(static path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var parentDirectory = Path.GetFileName(Path.GetDirectoryName(path));
                return string.Equals(fileName, parentDirectory, StringComparison.OrdinalIgnoreCase);
            })
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildRegistryContent(AssetRegistrySpec spec, IReadOnlyList<string> assets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("namespace Assets;");
        builder.AppendLine();
        builder.AppendLine($"public static class {spec.ClassName}");
        builder.AppendLine("{");

        if (assets.Count == 0)
        {
            builder.AppendLine("}");
            return builder.ToString();
        }

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in assets)
        {
            builder.AppendLine($"    @Asset(\"{asset}\")");
            builder.AppendLine($"    public static {spec.TypeName} {MakeUniqueIdentifier(asset, usedNames)} {{ get; set; }}");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string MakeUniqueIdentifier(string assetName, ISet<string> usedNames)
    {
        var builder = new StringBuilder(assetName.Length + 8);
        var capitalizeNext = true;

        foreach (var ch in assetName)
        {
            if (char.IsLetterOrDigit(ch))
            {
                var output = capitalizeNext ? char.ToUpperInvariant(ch) : ch;
                builder.Append(output);
                capitalizeNext = false;
                continue;
            }

            capitalizeNext = true;
        }

        if (builder.Length == 0)
            builder.Append("Asset");

        if (char.IsDigit(builder[0]))
            builder.Insert(0, '_');

        var candidate = builder.ToString();
        if (usedNames.Add(candidate))
            return candidate;

        var suffix = 2;
        while (usedNames.Add(candidate + suffix) is false)
            suffix++;

        return candidate + suffix;
    }
}

