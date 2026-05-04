using TypedGML.Compiler.Diagnostics;

namespace TypedGML.CLI;

internal static class DiagnosticPrinter
{
    public static void Print(DiagnosticBag diagnostics, TgmlConfig config, string bclPath)
    {
        foreach (var diagnostic in diagnostics.All)
        {
            var severity = diagnostic.Severity.ToString().ToLowerInvariant();
            var code = $"TGML{(int)diagnostic.Code:0000}";
            var location = FormatLocation(diagnostic.Location, config, bclPath);
            Console.Error.WriteLine($"{severity} {code}: {diagnostic.Message} ({location})");
        }
    }

    private static string FormatLocation(SourceLocation location, TgmlConfig config, string bclPath)
    {
        var path = FormatPath(location.FilePath, config, bclPath);
        return $"{path}:{location.Line}:{location.Column}";
    }

    private static string FormatPath(string path, TgmlConfig config, string bclPath)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "<unknown>";

        var fullPath = Path.GetFullPath(path);
        if (IsUnder(fullPath, config.TgmlRoot))
            return Normalize(Path.GetRelativePath(config.TgmlRoot, fullPath));

        if (IsUnder(fullPath, bclPath))
            return Normalize(Path.Combine("bcl", Path.GetRelativePath(bclPath, fullPath)));

        return Normalize(fullPath);
    }

    private static bool IsUnder(string path, string root)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return path.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/');
}
