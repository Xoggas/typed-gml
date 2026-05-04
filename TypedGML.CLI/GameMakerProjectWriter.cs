using System.Text;
using TypedGML.CLI.GameMaker;

namespace TypedGML.CLI;

internal sealed class GameMakerProjectWriter
{
    private const string RootFolderPath = "folders/TypedGML Generated.yy";
    private const string BclFolderPath = "folders/TypedGML Generated/BCL.yy";

    public void Write(CliCompileResult compileResult, string yypPath)
    {
        var yypFullPath = Path.GetFullPath(yypPath);
        var gmRoot = Path.GetDirectoryName(yypFullPath)!;
        var projectName = Path.GetFileNameWithoutExtension(yypFullPath);
        var yypFileName = Path.GetFileName(yypFullPath);
        var folders = FolderBuilder.Build(compileResult.Namespaces, projectName, yypFileName);

        WriteScripts(compileResult, gmRoot, folders);
        WriteObjects(compileResult, gmRoot, folders);
        new YypUpdater().Update(
            yypFullPath,
            compileResult.Scripts.Keys.ToList(),
            compileResult.Objects.Select(obj => obj.Name).ToList(),
            folders);
    }

    private static void WriteScripts(
        CliCompileResult compileResult,
        string gmRoot,
        IReadOnlyList<FolderEntry> folders)
    {
        var scriptWriter = new ScriptYyWriter();
        foreach (var (name, content) in compileResult.Scripts)
        {
            var gmlDest = Path.Combine(gmRoot, "scripts", name, $"{name}.gml");
            WriteText(gmlDest, content);
            scriptWriter.Write(name, gmRoot, GetFolderPath(name, compileResult, folders));
        }
    }

    private static void WriteObjects(
        CliCompileResult compileResult,
        string gmRoot,
        IReadOnlyList<FolderEntry> folders)
    {
        var objectWriter = new ObjectYyWriter();
        foreach (var obj in compileResult.Objects)
        {
            foreach (var (eventFile, content) in obj.Events)
                WriteText(Path.Combine(gmRoot, "objects", obj.Name, $"{eventFile}.gml"), content);

            objectWriter.Write(obj.Name, gmRoot, obj.Events.Keys.ToList(), GetFolderPath(obj.Name, compileResult, folders));
        }
    }

    private static string GetFolderPath(
        string resourceName,
        CliCompileResult compileResult,
        IReadOnlyList<FolderEntry> folders)
    {
        if (resourceName.StartsWith("TypedGML_", StringComparison.Ordinal))
            return ExistingFolderOrRoot(BclFolderPath, folders);

        if (compileResult.ResourceNamespaces.TryGetValue(resourceName, out var namespaceName) &&
            !string.IsNullOrWhiteSpace(namespaceName))
            return ExistingFolderOrRoot($"folders/TypedGML Generated/{namespaceName.Replace('.', '/')}.yy", folders);

        return RootFolderPath;
    }

    private static string ExistingFolderOrRoot(string folderPath, IReadOnlyList<FolderEntry> folders) =>
        folders.Any(folder => folder.FolderPath == folderPath) ? folderPath : RootFolderPath;

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }
}
