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

    public void WriteScript(string name, string content, Folder? parentFolder = null)
    {
        if (ScriptExists(name))
        {
            _changesTracker.RegisterScript(name);
            return;
        }

        if (parentFolder is null)
        {
            throw new ArgumentNullException(nameof(parentFolder));
        }

        var folderReference = Reference.FromFolder(parentFolder);

        var scriptMetadata = new Script
        {
            Name = name,
            InternalName = name,
            Parent = folderReference
        };

        var scriptFolderPathLocal = PathExtensions.Combine("scripts", name);
        var scriptFolderPath = PathExtensions.Combine(_projectPath, scriptFolderPathLocal);
        
        var scriptMetadataPathLocal = PathExtensions.Combine(scriptFolderPathLocal, name + ".yy");
        var scriptMetadataPath = PathExtensions.Combine(scriptFolderPath, name + ".yy");
        var scriptMetadataJson = JsonConvert.SerializeObject(scriptMetadata, Formatting.Indented);
        
        var scriptCodePath = PathExtensions.Combine(scriptFolderPath, name + ".gml");

        Directory.CreateDirectory(scriptFolderPath);
        File.WriteAllText(scriptMetadataPath, scriptMetadataJson);
        File.WriteAllText(scriptCodePath, content);

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

    private bool ScriptExists(string name)
    {
        var scriptPath = Path.Combine(_projectPath, "scripts", name, name + ".yy");
        return File.Exists(scriptPath);
    }
}