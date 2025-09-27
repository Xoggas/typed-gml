// const string tgmlProjectFileName = "tgmlMetadata.json";
//
// if (args.Length == 0)
// {
//     Console.WriteLine("Specify the project file path");
//     return;
// }
//
// var projectFolderPath = args[0];
// var projectFilePath = Path.Combine(projectFolderPath, tgmlProjectFileName);
//
// if (File.Exists(projectFilePath) is false)
// {
//     Console.WriteLine("Project file not found");
//     return;
// }
//
// var tgmlProjectFile = JsonConvert.DeserializeObject<TgmlProjectFile>(File.ReadAllText(projectFilePath));
//
// if (tgmlProjectFile is null)
// {
//     Console.WriteLine("Invalid project file");
//     return;
// }

using TypedGML.Transpiler.GameMaker;

const string projectPath = @"C:\Users\xogga\OneDrive\Documents\Projects\TestGame";
const string content = """
                       show_debug_message("I'm working!");
                       """;

var api = GameMakerApi.Init(projectPath);
api.FileManager.FolderModule.GetFolder("Scripts/Folder1");
api.FileManager.FolderModule.GetFolder("Scripts/Folder1/Folder2");

var folder = api.FileManager.FolderModule.GetFolder("Scripts/Folder1/Folder2/Folder3");
api.FileManager.ScriptModule.WriteScript("Script1", content, folder);

// api.WriteScript("Scripts/Folder1/Folder2", "Script2", content);
// api.WriteScript("Scripts/Folder1/", "Script1", content);
api.PerformCleanup();
api.CommitChanges();