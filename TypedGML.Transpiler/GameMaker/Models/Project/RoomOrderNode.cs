using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class RoomOrderNode
{
    [JsonPropertyName("roomId")]
    public Reference RoomId { get; set; } = new();
}