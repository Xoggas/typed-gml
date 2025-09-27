using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Event
{
    [JsonProperty("$GMEvent")]
    public string GameMakerEvent { get; set; } = "v1";

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("collisionObjectId")]
    public Reference? CollisionObjectId { get; set; }

    [JsonProperty("eventNum")]
    public int EventNumber { get; set; }

    [JsonProperty("eventType")]
    public EventType EventType { get; set; }

    [JsonProperty("isDnD")]
    public bool IsDnD { get; set; }

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMEvent";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";
}