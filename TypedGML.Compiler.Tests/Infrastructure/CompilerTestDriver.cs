using System.Diagnostics;

namespace TypedGML.Compiler.Tests.Infrastructure;

public static class CompilerTestDriver
{
    public static CompilationResult Compile(string source, string fileName = "main.tgml") =>
        Compile(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [fileName] = source
        });

    public static CompilationResult Compile(IReadOnlyDictionary<string, string> files)
    {
        var root = Path.Combine(Path.GetTempPath(), "typed-gml-tests", Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "input");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(input);

        foreach (var file in files)
        {
            var path = Path.Combine(input, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Value);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet", BuildArguments(input, output))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = SolutionRoot()
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CompilationResult(process.ExitCode, stdout, stderr, ReadFiles(output));
    }

    private static string BuildArguments(string input, string output) =>
        $"\"{CliPath()}\" \"{input}\" \"{output}\"";

    private static IReadOnlyDictionary<string, string> ReadFiles(string output)
    {
        if (!Directory.Exists(output))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return Directory.GetFiles(output, "*.gml", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(output, path).Replace('\\', '/'),
                File.ReadAllText,
                StringComparer.Ordinal);
    }

    private static string CliPath()
    {
        var cliRoot = Path.Combine(SolutionRoot(), "TypedGML.CLI", "bin");
        var matches = Directory.GetFiles(cliRoot, "TypedGML.CLI.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        return matches.Count > 0
            ? matches[0]
            : throw new InvalidOperationException("TypedGML.CLI.dll was not found. Build the solution before running tests.");
    }

    private static string SolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TypedGML.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the solution root.");
    }
}
