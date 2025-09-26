using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Object
{
    [JsonPropertyName("$GMObject")]
    public string GameMakerObject { get; set; } = string.Empty;

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<Event> Events { get; set; } = [];

    [JsonPropertyName("managed")]
    public bool IsManaged { get; set; }

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("overriddenProperties")]
    public List<object> OverriddenProperties { get; set; } = [];

    [JsonPropertyName("parent")]
    public Reference Parent { get; set; } = new();

    [JsonPropertyName("parentObjectId")]
    public Reference ParentObjectId { get; set; } = new();

    [JsonPropertyName("persistent")]
    public bool Persistent { get; set; }

    [JsonPropertyName("physicsAngularDamping")]
    public double PhysicsAngularDamping { get; set; }

    [JsonPropertyName("physicsDensity")]
    public double PhysicsDensity { get; set; }

    [JsonPropertyName("physicsFriction")]
    public double PhysicsFriction { get; set; }

    [JsonPropertyName("physicsGroup")]
    public int PhysicsGroup { get; set; }

    [JsonPropertyName("physicsKinematic")]
    public bool PhysicsKinematic { get; set; }

    [JsonPropertyName("physicsLinearDamping")]
    public double PhysicsLinearDamping { get; set; }

    [JsonPropertyName("physicsObject")]
    public bool PhysicsObject { get; set; }

    [JsonPropertyName("physicsRestitution")]
    public double PhysicsRestitution { get; set; }

    [JsonPropertyName("physicsSensor")]
    public bool PhysicsSensor { get; set; }

    [JsonPropertyName("physicsShape")]
    public int PhysicsShape { get; set; }

    [JsonPropertyName("physicsShapePoints")]
    public List<object> PhysicsShapePoints { get; set; } = [];

    [JsonPropertyName("physicsStartAwake")]
    public bool PhysicsStartAwake { get; set; }

    [JsonPropertyName("properties")]
    public List<object> Properties { get; set; } = [];

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = string.Empty;

    [JsonPropertyName("solid")]
    public bool Solid { get; set; }

    [JsonPropertyName("spriteId")]
    public Reference? SpriteId { get; set; }

    [JsonPropertyName("spriteMaskId")]
    public Reference? SpriteMaskId { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}