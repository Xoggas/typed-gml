using System.CommandLine;
using TypedGML.CLI.GameMaker;
using TypedGML.Transpiler;

namespace TypedGML.CLI.Commands;

public sealed class BuildProjectCommand : ICommand
{
    public Command BuildCommand()
    {
        var pathOption = new Option<string>("--path", "-p")
        {
            Required = true,
            Description = "Path to the GameMaker project folder"
        };

        pathOption.Validators.Add(result =>
        {
            var projectFolderPath = result.GetValue(pathOption);
            if (string.IsNullOrWhiteSpace(projectFolderPath) || Directory.Exists(projectFolderPath) is false)
                result.AddError("Directory doesn't exist.");
        });

        var buildCommand = new Command("build", "Builds TypedGML sources and writes the generated GameMaker resources");
        buildCommand.Options.Add(pathOption);
        buildCommand.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(pathOption);
            if (string.IsNullOrWhiteSpace(projectPath))
                return 1;

            return BuildProject(projectPath);
        });

        return buildCommand;
    }

    private static int BuildProject(string projectPath)
    {
        var gameMakerProject = GameMakerProject.Open(projectPath);
        gameMakerProject.EnsureTypedGmlSourceStructure();
        gameMakerProject.EnsureGeneratedFolders();

        AssetRegistryGenerator.Generate(gameMakerProject.ProjectRootPath, gameMakerProject.TypedGmlSourcePath);

        var sources = LoadSources(gameMakerProject.TypedGmlSourcePath);
        var result = TranspilerApi.Transpile(sources);

        if (result.Success is false)
        {
            foreach (var diagnostic in result.Diagnostics)
                Console.WriteLine(diagnostic);

            return 1;
        }

        var integrationErrors = new List<string>();

        foreach (var file in result.Files.Where(static file => file.Path.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase)))
        {
            gameMakerProject.UpsertScript(file);
        }

        foreach (var objectGroup in result.Files
                     .Where(static file => file.Path.StartsWith("Objects/", StringComparison.OrdinalIgnoreCase))
                     .GroupBy(static file => file.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)[1], StringComparer.OrdinalIgnoreCase))
        {
            if (gameMakerProject.TryUpsertObject(objectGroup.Key, objectGroup.ToArray(), out var error))
                continue;

            integrationErrors.Add(error!);
        }

        foreach (var file in result.Files)
        {
            if (file.Path.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase) ||
                file.Path.StartsWith("Objects/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            integrationErrors.Add($"Unsupported transpiler output path '{file.Path}'.");
        }

        if (integrationErrors.Count > 0)
        {
            foreach (var error in integrationErrors)
                Console.WriteLine(error);

            return 1;
        }

        gameMakerProject.Save();
        Console.WriteLine($"Build completed for '{gameMakerProject.ProjectRootPath}'.");
        return 0;
    }

    private static IReadOnlyList<TgmlSourceFile> LoadSources(string typedGmlSourcePath)
    {
        var sources = new List<TgmlSourceFile>();
        var bclPath = RuntimeLayout.GetBclPath();

        foreach (var path in Directory
                     .EnumerateFiles(bclPath, "*.tgml", SearchOption.AllDirectories)
                     .OrderBy(path => Path.GetRelativePath(bclPath, path), StringComparer.OrdinalIgnoreCase))
        {
            sources.Add(new TgmlSourceFile(Path.GetRelativePath(bclPath, path), File.ReadAllText(path)));
        }

        foreach (var path in Directory
                     .EnumerateFiles(typedGmlSourcePath, "*.tgml", SearchOption.AllDirectories)
                     .OrderBy(path => Path.GetRelativePath(typedGmlSourcePath, path), StringComparer.OrdinalIgnoreCase))
        {
            sources.Add(new TgmlSourceFile(Path.GetRelativePath(typedGmlSourcePath, path), File.ReadAllText(path)));
        }

        return sources;
    }
}

