using Newtonsoft.Json;
using TypedGML.Transpiler.GameMaker;

namespace TypedGML.CLI.GameMaker;

public class GameMakerEngine
{
    private readonly string _projectFilePath;
    private readonly Project _project;

    private GameMakerEngine(string projectFilePath, Project project)
    {
        _projectFilePath = projectFilePath;
        _project = project;
    }

    public static GameMakerEngine Create(string projectFilePath)
    {
        if (File.Exists(projectFilePath) is false)
        {
            throw new FileNotFoundException("Project file not found", projectFilePath);
        }

        if (Path.GetExtension(projectFilePath).Equals(".yyp") is false)
        {
            throw new Exception("Invalid project file format");
        }

        var projectFile = JsonConvert.DeserializeObject<Project>(File.ReadAllText(projectFilePath));

        if (projectFile is null)
        {
            throw new Exception("Invalid project file");
        }

        return new GameMakerEngine(projectFilePath, projectFile);
    }

    public void CommitChanges()
    {
        var json = JsonConvert.SerializeObject(_project, Formatting.Indented);

        File.WriteAllText(_projectFilePath, json);
    }

    public Reference GetOrCreateFolder(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path is null or empty");
        }

        var parts = path.Split('/');
        var pathAccumulator = "folders";
        var requestedFolder = default(Folder)!;

        foreach (var part in parts)
        {
            pathAccumulator = PathExtensions.Combine(pathAccumulator, part);

            var folder = _project.Folders.FirstOrDefault(f => f.Name.Equals(part));
            
            if (folder is not null)
            {
                requestedFolder = folder;
                continue;
            }

            folder = Folder.Create(part, pathAccumulator);

            _project.Folders.Add(folder);
        }

        return Reference.FromFolder(requestedFolder);
    }

    public void CreateScript()
    {
        throw new NotImplementedException();
    }
}