namespace TypedGML.CLI;

internal static class BclPathResolver
{
    public static string? Resolve()
    {
        var bclPath = Path.Combine(AppContext.BaseDirectory, "bcl");
        if (Directory.Exists(bclPath))
            return bclPath;

        Console.Error.WriteLine($"error: BCL folder not found: {bclPath}");
        return null;
    }
}
