using TypedGML.Transpiler;

var projectPath = FindProjectDirectory();
var resultPath = Path.Combine(projectPath, "Generated");
var bclPath = Path.Combine(projectPath, "Bcl");
var defaultScriptPath = Path.Combine(projectPath, "PositiveE2E.tgml");
var requestedScriptPath = args.Length > 0 ? args[0] : defaultScriptPath;
var scriptPath = Path.GetFullPath(requestedScriptPath, projectPath);

if (!File.Exists(scriptPath))
{
    throw new FileNotFoundException($"Could not find TypedGML script '{scriptPath}'.", scriptPath);
}

var scriptName = Path.GetRelativePath(projectPath, scriptPath);

var sources = Directory
    .EnumerateFiles(bclPath, "*.tgml", SearchOption.AllDirectories)
    .OrderBy(path => Path.GetRelativePath(bclPath, path), StringComparer.OrdinalIgnoreCase)
    .Select(path => new TgmlSourceFile(Path.GetRelativePath(bclPath, path), File.ReadAllText(path)))
    .Append(new TgmlSourceFile(scriptName, File.ReadAllText(scriptPath)))
    .ToArray();

var result = TranspilerApi.Transpile(sources);

if (result.Success is false)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }

    return;
}

foreach (var file in result.Files)
{
    var outputPath = Path.Combine(resultPath, file.Path);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException());
    File.WriteAllText(outputPath, file.Content);
}

return;

static string FindProjectDirectory()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "TypedGML.Transpiler.csproj")))
            return current.FullName;

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate TypedGML.Transpiler.csproj.");
}
