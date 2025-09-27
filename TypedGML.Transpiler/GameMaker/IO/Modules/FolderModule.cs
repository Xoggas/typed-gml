using System.Diagnostics.CodeAnalysis;

namespace TypedGML.Transpiler.GameMaker.IO;

public sealed class FolderModule
{
    private readonly Project _project;
    private readonly ChangesTracker _changesTracker;

    public FolderModule(Project project, ChangesTracker changesTracker)
    {
        _project = project;
        _changesTracker = changesTracker;
    }

    public bool TryFindFolder(string path, [NotNullWhen(true)] out Reference? folderReference)
    {
        var realPath = PathExtensions.Combine("folders", path) + ".yy";

        if (_project.FolderByPathLookup.TryGetValue(realPath, out var folder))
        {
            folderReference = Reference.FromFolder(folder);

            _changesTracker.RegisterFolder(path);

            return true;
        }

        folderReference = null;
        return false;
    }

    public Reference GetFolder(string path)
    {
        var parts = path.Split('/');

        if (parts.Length == 0)
        {
            throw new Exception("Invalid folder path");
        }

        var pathAccumulator = string.Empty;

        foreach (var part in parts)
        {
            pathAccumulator = PathExtensions.Combine(pathAccumulator, part);

            if (TryFindFolder(pathAccumulator, out _) is false)
            {
                var folder = Folder.Create(part, pathAccumulator);
                _project.Folders.Add(folder);
                _project.FolderByPathLookup[folder.FolderPath] = folder;
            }

            _changesTracker.RegisterFolder(pathAccumulator);
        }

        var resultingPath = PathExtensions.Combine("folders", pathAccumulator) + ".yy";

        return Reference.FromFolder(_project.FolderByPathLookup[resultingPath]);
    }

    public void DeleteFolder(string path)
    {
        var realFolderPath = PathExtensions.Combine("folders", path) + ".yy";

        if (_project.FolderByPathLookup.TryGetValue(realFolderPath, out Folder? folder))
        {
            _project.Folders.Remove(folder);
            _project.FolderByPathLookup.Remove(folder.FolderPath);
        }
    }
}