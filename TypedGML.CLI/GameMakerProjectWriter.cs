using System.Text;
using TypedGML.CLI.GameMaker;

namespace TypedGML.CLI;

internal sealed class GameMakerProjectWriter
{
    public void Write(CliCompileResult compileResult, string yypPath, string tgmlRoot)
    {
        var yypFullPath = Path.GetFullPath(yypPath);
        var gmRoot = Path.GetDirectoryName(yypFullPath)!;
        var projectName = Path.GetFileNameWithoutExtension(yypFullPath);
        var yypFileName = Path.GetFileName(yypFullPath);

        WriteScripts(compileResult, gmRoot, projectName, yypFileName, tgmlRoot);
        WriteObjects(compileResult, gmRoot, projectName, yypFileName, tgmlRoot);
        var folders = BuildFolders(compileResult, projectName, yypFileName, tgmlRoot);
        new YypUpdater().Update(
            yypFullPath,
            compileResult.Scripts.Keys.ToList(),
            compileResult.BclScriptNames,
            compileResult.Objects.Select(obj => obj.Name).ToList(),
            folders);
    }

    private static void WriteScripts(
        CliCompileResult compileResult,
        string gmRoot,
        string projectName,
        string yypFileName,
        string tgmlRoot)
    {
        var scriptWriter = new ScriptYyWriter();
        foreach (var (name, content) in compileResult.Scripts)
        {
            var gmlDest = Path.Combine(gmRoot, "scripts", name, $"{name}.gml");
            WriteText(gmlDest, content);
            var isBcl = compileResult.BclScriptNames.Contains(name);
            var gmFolder = FolderForResource(compileResult, name, isBcl, tgmlRoot);
            scriptWriter.Write(name, gmRoot, projectName, yypFileName, isBcl, gmFolder);
        }
    }

    private static void WriteObjects(
        CliCompileResult compileResult,
        string gmRoot,
        string projectName,
        string yypFileName,
        string tgmlRoot)
    {
        var objectWriter = new ObjectYyWriter();
        foreach (var obj in compileResult.Objects)
        {
            foreach (var (eventFile, content) in obj.Events)
                WriteText(Path.Combine(gmRoot, "objects", obj.Name, $"{eventFile}.gml"), content);

            var gmFolder = FolderForSource(obj.SourceFilePath, tgmlRoot);
            objectWriter.Write(obj.Name, gmRoot, obj.Events.Keys.ToList(), projectName, yypFileName, gmFolder);
        }
    }

    private static IReadOnlyList<FolderEntry> BuildFolders(
        CliCompileResult compileResult,
        string projectName,
        string yypFileName,
        string tgmlRoot)
    {
        var folderPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in compileResult.Scripts.Keys)
        {
            var isBcl = compileResult.BclScriptNames.Contains(name);
            var folder = FolderForResource(compileResult, name, isBcl, tgmlRoot);
            if (folder is not null)
                folderPaths.Add(folder);
        }

        foreach (var obj in compileResult.Objects)
        {
            var folder = FolderForSource(obj.SourceFilePath, tgmlRoot);
            if (folder is not null)
                folderPaths.Add(folder);
        }

        return FolderBuilder.Build(folderPaths, projectName, yypFileName);
    }

    private static string? FolderForResource(
        CliCompileResult compileResult,
        string name,
        bool isBcl,
        string tgmlRoot)
    {
        if (isBcl)
            return GameMakerFolderPath.Bcl;

        return compileResult.ResourceSourcePaths.TryGetValue(name, out var sourcePath)
            ? FolderForSource(sourcePath, tgmlRoot)
            : null;
    }

    private static string? FolderForSource(string? sourceFilePath, string tgmlRoot) =>
        sourceFilePath is null ? null : GameMakerFolderPath.FromSource(sourceFilePath, tgmlRoot);

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }
}
