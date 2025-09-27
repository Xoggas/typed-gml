using Newtonsoft.Json;
using TypedGML.CLI;
using TypedGML.CLI.GameMaker;

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

const string projectFilePath = @"C:\Users\xogga\OneDrive\Documents\Projects\TestGame\TestGame.yyp";

var gameMaker = GameMakerEngine.Create(projectFilePath);

gameMaker.GetOrCreateFolder("Scripts/System/Collections/Generic");

gameMaker.CommitChanges();