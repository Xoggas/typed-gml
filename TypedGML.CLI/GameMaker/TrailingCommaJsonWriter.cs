using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class TrailingCommaJsonWriter
{
    private readonly StringBuilder _builder = new();

    public string Write(JsonObject root)
    {
        WriteNode(root, 0);
        _builder.Append('\n');
        return _builder.ToString();
    }

    private void WriteNode(JsonNode? node, int indent)
    {
        switch (node)
        {
            case null:
                _builder.Append("null");
                break;
            case JsonObject obj:
                WriteObject(obj, indent);
                break;
            case JsonArray array:
                WriteArray(array, indent);
                break;
            default:
                _builder.Append(node.ToJsonString());
                break;
        }
    }

    private void WriteObject(JsonObject obj, int indent)
    {
        if (obj.Count == 0)
        {
            _builder.Append("{}");
            return;
        }

        _builder.Append("{\n");
        foreach (var property in obj)
        {
            Indent(indent + 2);
            _builder.Append(JsonSerializer.Serialize(property.Key));
            _builder.Append(": ");
            WriteNode(property.Value, indent + 2);
            _builder.Append(",\n");
        }

        Indent(indent);
        _builder.Append('}');
    }

    private void WriteArray(JsonArray array, int indent)
    {
        if (array.Count == 0)
        {
            _builder.Append("[]");
            return;
        }

        _builder.Append("[\n");
        foreach (var item in array)
        {
            Indent(indent + 2);
            WriteNode(item, indent + 2);
            _builder.Append(",\n");
        }

        Indent(indent);
        _builder.Append(']');
    }

    private void Indent(int count) =>
        _builder.Append(' ', count);
}
