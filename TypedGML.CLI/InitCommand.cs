using System.Text;
using System.Text.Json;

namespace TypedGML.CLI;

internal static class InitCommand
{
    public static int Run(string[] args)
    {
        if (BclPathResolver.Resolve() is null)
            return 1;

        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: typedgml init <yyp-path>");
            return 1;
        }

        var yypPath = Path.GetFullPath(args[0]);
        if (!File.Exists(yypPath))
        {
            Console.Error.WriteLine($"error: GameMaker project file not found: {yypPath}");
            return 1;
        }

        WriteConfig(yypPath);
        return 0;
    }

    private static void WriteConfig(string yypPath)
    {
        var gmRoot = Path.GetDirectoryName(yypPath)!;
        var tgmlRoot = Path.Combine(gmRoot, "tgml");
        var configPath = Path.Combine(tgmlRoot, "tgml.config.json");
        Directory.CreateDirectory(tgmlRoot);

        var config = new
        {
            project = Normalize(Path.GetRelativePath(tgmlRoot, yypPath)),
            sources = new[] { "**/*.tgml" }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json + Environment.NewLine, new UTF8Encoding(false));
        PrintInitialized(configPath);
    }

    private static void PrintInitialized(string configPath)
    {
        Console.WriteLine("TypedGML initialized.");
        Console.WriteLine($"Config: {configPath}");
        Console.WriteLine("Place your .tgml source files anywhere inside tgml/ and run:");
        Console.WriteLine("  typedgml build");
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/');
}
