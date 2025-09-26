using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Event
{
    [JsonPropertyName("$GMEvent")]
    public string GameMakerEvent { get; set; } = "v1";

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("collisionObjectId")]
    public Reference? CollisionObjectId { get; set; }

    [JsonPropertyName("eventNum")]
    public int EventNumber { get; set; }

    [JsonPropertyName("eventType")]
    [JsonConverter(typeof(JsonNumberEnumConverter<EventType>))]
    public EventType EventType { get; set; }

    [JsonPropertyName("isDnD")]
    public bool IsDnD { get; set; }

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMEvent";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";
}