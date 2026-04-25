namespace TypedGML.CLI.GameMaker;

internal static class RuntimeLayout
{
    public static string GetBclPath()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, "BCL");
        if (Directory.Exists(bundledPath))
            return bundledPath;

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "TypedGML.Transpiler", "BCL");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the TypedGML base class library.");
    }
}

