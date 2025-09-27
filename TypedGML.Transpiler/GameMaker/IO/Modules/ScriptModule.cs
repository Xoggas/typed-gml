using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker.IO;

public sealed class ScriptModule
{
    private readonly string _projectPath;
    private readonly Project _project;
    private readonly ChangesTracker _changesTracker;

    public ScriptModule(string projectPath, Project project, ChangesTracker changesTracker)
    {
        _projectPath = projectPath;
        _project = project;
        _changesTracker = changesTracker;
    }

    public void WriteScript(string name, string content, Folder parentFolder)
    {
        var folderReference = Reference.FromFolder(parentFolder);
        var scriptFolderPathLocal = PathExtensions.Combine("scripts", name);
        var scriptFolderPath = PathExtensions.Combine(_projectPath, scriptFolderPathLocal);
        var scriptMetadataPathLocal = PathExtensions.Combine(scriptFolderPathLocal, name + ".yy");
        var scriptMetadataPath = PathExtensions.Combine(scriptFolderPath, name + ".yy");
        var scriptCodePath = PathExtensions.Combine(scriptFolderPath, name + ".gml");

        if (TryFindScript(name, out Script? script) is false)
        {
            Directory.CreateDirectory(scriptFolderPath);

            script = new Script
            {
                Name = name,
                InternalName = name,
                Parent = folderReference
            };

            var scriptFileReference = new Reference
            {
                Name = name,
                Path = scriptMetadataPathLocal
            };

            var resourceEntry = new ResourceEntry
            {
                Id = scriptFileReference
            };

            _project.Resources.Add(resourceEntry);
            _project.ResourceByNameLookup.Add(name, resourceEntry);
        }
        else
        {
            script.Parent = folderReference;
        }

        var scriptJson = JsonConvert.SerializeObject(script, Formatting.Indented);

        File.WriteAllText(scriptMetadataPath, scriptJson);
        File.WriteAllText(scriptCodePath, content);

        _changesTracker.RegisterScript(name);
    }

    public void DeleteScript(string name)
    {
        var scriptFolderPath = Path.Combine(_projectPath, "scripts", name);

        if (Directory.Exists(scriptFolderPath) is false)
        {
            return;
        }

        _project.Resources.RemoveAll(x => x.Id.Name.Equals(name));
        _project.ResourceByNameLookup.Remove(name);

        Directory.Delete(scriptFolderPath, true);
    }

    private bool TryFindScript(string name, [NotNullWhen(true)] out Script? script)
    {
        var scriptPath = Path.Combine(_projectPath, "scripts", name, name + ".yy");

        if (File.Exists(scriptPath) is false)
        {
            script = null;
            return false;
        }

        script = JsonConvert.DeserializeObject<Script>(File.ReadAllText(scriptPath));

        return script is not null;
    }
}