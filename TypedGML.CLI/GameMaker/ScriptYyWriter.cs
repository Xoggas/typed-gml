using System.Text;

namespace TypedGML.CLI.GameMaker;

internal sealed class ScriptYyWriter
{
    public void Write(
        string scriptName,
        string gmProjectRoot,
        string projectName,
        string yypFileName,
        bool isBcl = false,
        string? gmFolder = null)
    {
        var path = Path.Combine(gmProjectRoot, "scripts", scriptName, $"{scriptName}.yy");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildContent(scriptName, isBcl, gmFolder, projectName, yypFileName), new UTF8Encoding(false));
    }

    private static string BuildContent(
        string scriptName,
        bool isBcl,
        string? gmFolder,
        string projectName,
        string yypFileName)
    {
        var parent = GetParentForScript(isBcl, gmFolder, projectName, yypFileName);
        return $$"""
        {
          "$GMScript":"v1",
          "%Name":"{{Escape(scriptName)}}",
          "isCompatibility":false,
          "isDnD":false,
          "name":"{{Escape(scriptName)}}",
          "parent":{
            "name":"{{Escape(parent.Name)}}",
            "path":"{{Escape(NormalizePath(parent.Path))}}",
          },
          "resourceType":"GMScript",
          "resourceVersion":"2.0",
        }
        """.ReplaceLineEndings("\n");
    }

    private static (string Name, string Path) GetParentForScript(
        bool isBcl,
        string? gmFolder,
        string projectName,
        string yypFileName) =>
        GameMakerFolderPath.AssetParent(isBcl ? GameMakerFolderPath.Bcl : gmFolder, projectName, yypFileName);

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
