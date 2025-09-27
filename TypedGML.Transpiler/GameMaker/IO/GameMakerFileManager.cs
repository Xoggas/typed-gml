using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker.IO;

public sealed class GameMakerFileManager
{
    public FolderModule FolderModule { get; }
    public ScriptModule ScriptModule { get; }

    private readonly string _projectFilePath;
    private readonly Project _project;

    public GameMakerFileManager(string projectFilePath, Project project, ChangesTracker changesTracker)
    {
        _projectFilePath = projectFilePath;
        _project = project;
        FolderModule = new FolderModule(project, changesTracker);
        ScriptModule = new ScriptModule(project, changesTracker);
    }

    public void CommitChanges()
    {
        var json = JsonConvert.SerializeObject(_project, Formatting.Indented);

        File.WriteAllText(_projectFilePath, json);
    }
}