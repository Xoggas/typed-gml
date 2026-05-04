using System.Text.Json;

namespace TypedGML.CLI;

internal static class BuildCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryParseWatch(args, out var watch))
            return 1;

        var bclPath = BclPathResolver.Resolve();
        if (bclPath is null)
            return 1;

        var configPath = ConfigFinder.Find(Environment.CurrentDirectory);
        if (configPath is null)
        {
            Console.Error.WriteLine("error: tgml.config.json not found. Run typedgml init <yyp-path> first.");
            return 1;
        }

        try
        {
            var config = ConfigLoader.Load(configPath);
            var runner = new BuildRunner(config, bclPath);
            if (!watch)
                return runner.Run();

            runner.Run();
            return await new WatchRunner(config.TgmlRoot, runner).RunAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static bool TryParseWatch(string[] args, out bool watch)
    {
        watch = false;
        if (args.Length == 0)
            return true;

        if (args.Length == 1 && args[0] == "--watch")
        {
            watch = true;
            return true;
        }

        Console.Error.WriteLine("Usage: typedgml build [--watch]");
        return false;
    }
}
