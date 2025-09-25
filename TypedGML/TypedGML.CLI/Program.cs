using System.Reflection;
using Newtonsoft.Json;
using TypedGML.CLI;
using TypedGML.CLI.Extensions;

const string helpCommand = "help";
const string initCommand = "init";
const string compileCommand = "compile";
string cliDirectoryPath =
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
string baseClassLibraryPath = Path.Combine(cliDirectoryPath, "BaseClassLibrary");

if (args.Length == 0)
{
    Console.WriteLine("Usage: tgml <command> <arguments>");
}

var command = args[0];

switch (command)
{
    case initCommand:
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: tgml init <path_to_gms_project>");
            return;
        }

        var path = args[1];
        InitializeProject(path);
        break;
    }
}

void InitializeProject(string path)
{
    var folderHasMetadataFile = Directory.GetFiles(path, "tgmlMetadata.json").Length != 0;

    if (folderHasMetadataFile)
    {
        Console.WriteLine("Project already initialized.");
        return;
    }

    var folderHasProjectFile = Directory.GetFiles(path, "*.yyp").Length != 0;

    if (folderHasProjectFile is false)
    {
        Console.WriteLine("Project not found.");
        return;
    }

    var tgmlMetadata = new Metadata();
    var tgmlMetadataPath = Path.Combine(path, "tgmlMetadata.json");
    var json = JsonConvert.SerializeObject(tgmlMetadata, Formatting.Indented);
    File.WriteAllText(tgmlMetadataPath, json);

    var tgmlBaseClassLibraryPath = Path.Combine(path, "tgml", "BaseClassLibrary");
    Directory.CreateDirectory(tgmlBaseClassLibraryPath);
    FileExtensions.CopyFilesRecursively(baseClassLibraryPath, tgmlBaseClassLibraryPath);

    Console.WriteLine("Project initialized.");
}