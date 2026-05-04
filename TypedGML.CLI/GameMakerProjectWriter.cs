using System.Text;
using TypedGML.CLI.GameMaker;

namespace TypedGML.CLI;

internal sealed class GameMakerProjectWriter
{
    public void Write(CliCompileResult compileResult, string yypPath)
    {
        var yypFullPath = Path.GetFullPath(yypPath);
        var gmRoot = Path.GetDirectoryName(yypFullPath)!;
        var projectName = Path.GetFileNameWithoutExtension(yypFullPath);
        var yypFileName = Path.GetFileName(yypFullPath);

        WriteScripts(compileResult, gmRoot, projectName, yypFileName);
        WriteObjects(compileResult, gmRoot, projectName, yypFileName);
        new YypUpdater().Update(
            yypFullPath,
            compileResult.Scripts.Keys.ToList(),
            compileResult.Objects.Select(obj => obj.Name).ToList(),
            []);
    }

    private static void WriteScripts(
        CliCompileResult compileResult,
        string gmRoot,
        string projectName,
        string yypFileName)
    {
        var scriptWriter = new ScriptYyWriter();
        foreach (var (name, content) in compileResult.Scripts)
        {
            var gmlDest = Path.Combine(gmRoot, "scripts", name, $"{name}.gml");
            WriteText(gmlDest, content);
            scriptWriter.Write(name, gmRoot, projectName, yypFileName);
        }
    }

    private static void WriteObjects(
        CliCompileResult compileResult,
        string gmRoot,
        string projectName,
        string yypFileName)
    {
        var objectWriter = new ObjectYyWriter();
        foreach (var obj in compileResult.Objects)
        {
            foreach (var (eventFile, content) in obj.Events)
                WriteText(Path.Combine(gmRoot, "objects", obj.Name, $"{eventFile}.gml"), content);

            objectWriter.Write(obj.Name, gmRoot, obj.Events.Keys.ToList(), projectName, yypFileName);
        }
    }

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }
}
