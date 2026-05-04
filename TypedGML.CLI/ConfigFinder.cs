namespace TypedGML.CLI;

internal static class ConfigFinder
{
    public static string? Find(string startDir)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDir)); directory is not null; directory = directory.Parent)
        {
            var configPath = Path.Combine(directory.FullName, "tgml.config.json");
            if (File.Exists(configPath))
                return configPath;
        }

        return null;
    }
}
