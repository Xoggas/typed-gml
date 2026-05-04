using System.Text;
using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypFolderArrayWriter
{
    private readonly StringBuilder _builder = new();

    public string Write(IReadOnlyList<JsonNode?> folders)
    {
        if (folders.Count == 0)
            return "[]";

        _builder.Append("[\n");
        foreach (var folder in folders)
            WriteFolder(folder);
        _builder.Append("  ]");
        return _builder.ToString();
    }

    private void WriteFolder(JsonNode? folder)
    {
        if (folder is JsonObject obj && IsGameMakerFolder(obj))
        {
            WriteGameMakerFolder(obj);
            return;
        }

        _builder.Append("    ");
        _builder.Append(folder?.ToJsonString() ?? "null");
        _builder.Append(",\n");
    }

    private void WriteGameMakerFolder(JsonObject folder)
    {
        _builder.Append("    {\n");
        WriteString("$GMFolder", StringValue(folder["$GMFolder"]) ?? string.Empty);
        WriteString("%Name", StringValue(folder["%Name"]) ?? string.Empty);
        WriteString("folderPath", StringValue(folder["folderPath"]) ?? string.Empty);
        WriteString("name", StringValue(folder["name"]) ?? string.Empty);
        WriteString("resourceType", StringValue(folder["resourceType"]) ?? "GMFolder");
        WriteString("resourceVersion", StringValue(folder["resourceVersion"]) ?? "2.0");
        _builder.Append("    },\n");
    }

    private void WriteString(string name, string value, int indent = 6)
    {
        _builder.Append(' ', indent);
        _builder.Append('"');
        _builder.Append(Escape(name));
        _builder.Append("\":\"");
        _builder.Append(Escape(value));
        _builder.Append("\",\n");
    }

    private static bool IsGameMakerFolder(JsonObject folder) =>
        folder.ContainsKey("$GMFolder") && folder.ContainsKey("%Name");

    private static string? StringValue(JsonNode? node) =>
        node?.GetValue<string>();

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
