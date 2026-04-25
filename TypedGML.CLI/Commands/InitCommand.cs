using System.CommandLine;
using TypedGML.CLI.GameMaker;

namespace TypedGML.CLI.Commands;

public class InitCommand : ICommand
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

        var initCommand = new Command("init", "Initializes TypedGML source folders inside a GameMaker project");
        initCommand.Options.Add(pathOption);
        initCommand.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(pathOption);
            if (string.IsNullOrWhiteSpace(projectPath))
                return 1;

            var gameMakerProject = GameMakerProject.Open(projectPath);
            gameMakerProject.EnsureTypedGmlSourceStructure();
            gameMakerProject.EnsureGeneratedFolders();
            AssetRegistryGenerator.Generate(gameMakerProject.ProjectRootPath, gameMakerProject.TypedGmlSourcePath);
            gameMakerProject.Save();

            Console.WriteLine($"Initialized TypedGML in '{gameMakerProject.ProjectRootPath}'.");
            return 0;
        });

        return initCommand;
    }
}