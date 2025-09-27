using Newtonsoft.Json;
using TypedGML.Transpiler.GameMaker.IO;

namespace TypedGML.Transpiler.GameMaker;

public sealed class GameMakerApi
{
    public GameMakerFileManager FileManager => _fileManager;

    private readonly GameMakerFileManager _fileManager;
    private readonly ChangesTracker _changesTracker;

    private GameMakerApi(GameMakerFileManager fileManager, ChangesTracker changesTracker)
    {
        _fileManager = fileManager;
        _changesTracker = changesTracker;
    }

    public static GameMakerApi Init(string projectPath)
    {
        if (Directory.Exists(projectPath) is false)
        {
            throw new DirectoryNotFoundException(projectPath);
        }

        var yypFilePath = Directory.GetFiles(projectPath, "*.yyp").FirstOrDefault();
        if (yypFilePath is null)
        {
            throw new Exception("Directory doesn't contain yyp file.");
        }

        // TODO: Fix formatting
        var changesFilePath = Path.Combine(projectPath, "changes.json");
        var changesFile = (File.Exists(changesFilePath)
            ? JsonConvert.DeserializeObject<Changes>(File.ReadAllText(changesFilePath), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            })
            : new Changes()) ?? new Changes();

        var projectFile = JsonConvert.DeserializeObject<Project>(File.ReadAllText(yypFilePath));
        if (projectFile is null)
        {
            throw new Exception("Project file is invalid.");
        }

        var changesTracker = new ChangesTracker(changesFilePath, changesFile);
        var fileManager = new GameMakerFileManager(yypFilePath, projectFile, changesTracker);
        return new GameMakerApi(fileManager, changesTracker);
    }

    public void WriteScript(string path, string name, string content)
    {
        throw new NotImplementedException();
    }

    public void PerformCleanup()
    {
        foreach (var entry in _changesTracker.DeadEntries)
        {
            switch (entry)
            {
                case FolderEntry folderEntry:
                {
                    _fileManager.FolderModule.DeleteFolder(folderEntry.Path);
                    break;
                }
                case ScriptEntry scriptEntry:
                {
                    _fileManager.ScriptModule.DeleteScript(scriptEntry.Name);
                    break;
                }
            }
        }
    }

    public void CommitChanges()
    {
        _changesTracker.Save();
        _fileManager.CommitChanges();
    }
}