using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TypedGML.CLI.GameMaker;

internal static class YypArrayLoader
{
    public static JsonArray Load(string projectText, string propertyName)
    {
        var cleaned = Regex.Replace(projectText, @",(\s*[}\]])", "$1");
        using var document = JsonDocument.Parse(cleaned);
        var root = JsonNode.Parse(document.RootElement.GetRawText()) as JsonObject;
        return root?[propertyName] as JsonArray ?? [];
    }
}
