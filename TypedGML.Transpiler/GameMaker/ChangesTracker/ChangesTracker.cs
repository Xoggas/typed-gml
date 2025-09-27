using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class ChangesTracker
{
    private readonly string _changesFilePath;
    private readonly HashSet<IEntry> _oldEntries;
    private readonly HashSet<IEntry> _newEntries = [];

    public ChangesTracker(string changesFilePath, Changes oldChanges)
    {
        _changesFilePath = changesFilePath;
        _oldEntries = oldChanges.Entries;
    }

    public IEnumerable<IEntry> DeadEntries => _oldEntries.Except(_newEntries);

    public void RegisterFolder(string name)
    {
        _newEntries.Add(new FolderEntry(name));
    }

    public void RegisterScript(string name)
    {
        _newEntries.Add(new ScriptEntry(name));
    }

    public void Save()
    {
        var changes = new Changes
        {
            Entries = _newEntries
        };

        var json = JsonConvert.SerializeObject(changes, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        File.WriteAllText(_changesFilePath, json);
    }
}