using System.Text;
using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypResourceArrayWriter
{
    private readonly StringBuilder _builder = new();

    public string Write(JsonArray resources)
    {
        if (resources.Count == 0)
            return "[]";

        _builder.Append("[\n");
        foreach (var resource in resources)
            WriteResource(resource);
        _builder.Append("  ]");
        return _builder.ToString();
    }

    private void WriteResource(JsonNode? resource)
    {
        var name = Escape(resource?["id"]?["name"]?.GetValue<string>() ?? string.Empty);
        var path = Escape(resource?["id"]?["path"]?.GetValue<string>() ?? string.Empty);
        _builder.Append("    {\"id\":{\"name\":\"");
        _builder.Append(name);
        _builder.Append("\",\"path\":\"");
        _builder.Append(path);
        _builder.Append("\",},},\n");
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
