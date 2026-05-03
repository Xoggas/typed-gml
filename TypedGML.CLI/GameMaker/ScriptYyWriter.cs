using System.Text;

namespace TypedGML.CLI.GameMaker;

internal class ScriptYyWriter
{
    public void Write(
        string scriptName,
        string gmProjectRoot,
        string parentFolderPath)
    {
        var path = Path.Combine(gmProjectRoot, "scripts", scriptName, $"{scriptName}.yy");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildContent(scriptName, parentFolderPath), Encoding.UTF8);
    }

    private static string BuildContent(string scriptName, string parentFolderPath) =>
        $$"""
        {
          "resourceType": "GMScript",
          "resourceVersion": "1.0",
          "name": "{{Escape(scriptName)}}",
          "isDnD": false,
          "isCompatibility": false,
          "parent": {
            "name": "{{Escape(Path.GetFileNameWithoutExtension(parentFolderPath))}}",
            "path": "{{Escape(NormalizePath(parentFolderPath))}}",
          },
          "tags": [],
        }
        """.ReplaceLineEndings("\n");

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
