using System.Text;

namespace TypedGML.CLI.GameMaker;

internal sealed class ScriptYyWriter
{
    public void Write(
        string scriptName,
        string gmProjectRoot,
        string projectName,
        string yypFileName)
    {
        var path = Path.Combine(gmProjectRoot, "scripts", scriptName, $"{scriptName}.yy");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildContent(scriptName, projectName, yypFileName), new UTF8Encoding(false));
    }

    private static string BuildContent(string scriptName, string projectName, string yypFileName) =>
        $$"""
        {
          "$GMScript":"v1",
          "%Name":"{{Escape(scriptName)}}",
          "isCompatibility":false,
          "isDnD":false,
          "name":"{{Escape(scriptName)}}",
          "parent":{
            "name":"{{Escape(projectName)}}",
            "path":"{{Escape(NormalizePath(yypFileName))}}",
          },
          "resourceType":"GMScript",
          "resourceVersion":"2.0",
        }
        """.ReplaceLineEndings("\n");

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
