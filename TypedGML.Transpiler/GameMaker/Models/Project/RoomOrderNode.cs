using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class RoomOrderNode
{
    [JsonProperty("roomId")]
    public Reference RoomId { get; set; } = new();
}