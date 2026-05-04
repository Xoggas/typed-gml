using System.Text.Json;

namespace TypedGML.CLI;

internal record TgmlConfig(
    string ProjectPath,
    string[] Sources,
    string TgmlRoot,
    string GmRoot,
    string YypPath);

internal static class ConfigLoader
{
    public static TgmlConfig Load(string configFilePath)
    {
        var configFullPath = Path.GetFullPath(configFilePath);
        var tgmlRoot = Path.GetDirectoryName(configFullPath)
            ?? throw new InvalidDataException("Invalid config path.");

        var rawConfig = ReadRawConfig(configFullPath);
        var yypPath = Path.GetFullPath(Path.Combine(tgmlRoot, rawConfig.Project));
        var gmRoot = Path.GetDirectoryName(yypPath)
            ?? throw new InvalidDataException("Invalid project path in tgml.config.json.");

        if (!File.Exists(yypPath))
            throw new FileNotFoundException($"GameMaker project file not found: {yypPath}");

        return new TgmlConfig(yypPath, rawConfig.Sources, tgmlRoot, gmRoot, yypPath);
    }

    private static RawConfig ReadRawConfig(string configFullPath)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawConfig = JsonSerializer.Deserialize<RawConfig>(File.ReadAllText(configFullPath), options);
        if (rawConfig is null || string.IsNullOrWhiteSpace(rawConfig.Project) || rawConfig.Sources is null)
            throw new InvalidDataException("tgml.config.json must contain project and sources.");

        if (rawConfig.Sources.Length == 0 || rawConfig.Sources.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("tgml.config.json sources must contain at least one glob pattern.");

        return rawConfig;
    }

    private sealed record RawConfig(string Project, string[] Sources);
}
