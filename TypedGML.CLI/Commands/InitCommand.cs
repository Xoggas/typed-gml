using System.CommandLine;
using System.Reflection;
using Newtonsoft.Json;
using TypedGML.CLI.Extensions;

namespace TypedGML.CLI.Commands;

public class InitCommand : ICommand
{
    private static string CliDirectoryPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
                                              Environment.CurrentDirectory;

    private static string BaseClassLibraryPath => Path.Combine(CliDirectoryPath, "BaseClassLibrary");

    public Command BuildCommand()
    {
        var pathOption = new Option<string>("--path", "-p")
        {
            Required = true,
            Description = "Path to the GameMaker project folder",
        };

        pathOption.Validators.Add(result =>
        {
            var projectFolderPath = result.GetValue(pathOption);

            if (Directory.Exists(projectFolderPath) is false)
            {
                result.AddError("Directory doesn't exist");
            }
        });

        var initCommand = new Command("init", "Initializes the project");
        initCommand.Options.Add(pathOption);
        initCommand.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(pathOption);

            if (projectPath is null)
            {
                return 1;
            }

            InitializeProject(projectPath);
            return 0;
        });

        return initCommand;
    }

    private static void InitializeProject(string projectPath)
    {
        var folderHasMetadataFile = Directory.GetFiles(projectPath, "tgmlMetadata.json").Length != 0;

        if (folderHasMetadataFile)
        {
            Console.WriteLine("Project already initialized");
            return;
        }

        var folderHasProjectFile = Directory.GetFiles(projectPath, "*.yyp").Length != 0;

        if (folderHasProjectFile is false)
        {
            Console.WriteLine("Project not found");
            return;
        }

        var tgmlMetadata = new Metadata();
        var tgmlMetadataPath = Path.Combine(projectPath, "tgmlMetadata.json");
        var json = JsonConvert.SerializeObject(tgmlMetadata, Formatting.Indented);
        File.WriteAllText(tgmlMetadataPath, json);

        var tgmlBaseClassLibraryPath = Path.Combine(projectPath, "tgml", "BaseClassLibrary");
        Directory.CreateDirectory(tgmlBaseClassLibraryPath);
        FileExtensions.CopyFilesRecursively(BaseClassLibraryPath, tgmlBaseClassLibraryPath);

        Console.WriteLine("Project initialized");
    }
}