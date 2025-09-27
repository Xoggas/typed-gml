using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Object
{
    [JsonProperty("$GMObject")]
    public string GameMakerObject { get; set; } = string.Empty;

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("eventList")]
    public List<Event> Events { get; set; } = [];

    [JsonProperty("managed")]
    public bool IsManaged { get; set; }

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("overriddenProperties")]
    public List<object> OverriddenProperties { get; set; } = [];

    [JsonProperty("parent")]
    public Reference Parent { get; set; } = new();

    [JsonProperty("parentObjectId")]
    public Reference ParentObjectId { get; set; } = new();

    [JsonProperty("persistent")]
    public bool Persistent { get; set; }

    [JsonProperty("physicsAngularDamping")]
    public double PhysicsAngularDamping { get; set; }

    [JsonProperty("physicsDensity")]
    public double PhysicsDensity { get; set; }

    [JsonProperty("physicsFriction")]
    public double PhysicsFriction { get; set; }

    [JsonProperty("physicsGroup")]
    public int PhysicsGroup { get; set; }

    [JsonProperty("physicsKinematic")]
    public bool PhysicsKinematic { get; set; }

    [JsonProperty("physicsLinearDamping")]
    public double PhysicsLinearDamping { get; set; }

    [JsonProperty("physicsObject")]
    public bool PhysicsObject { get; set; }

    [JsonProperty("physicsRestitution")]
    public double PhysicsRestitution { get; set; }

    [JsonProperty("physicsSensor")]
    public bool PhysicsSensor { get; set; }

    [JsonProperty("physicsShape")]
    public int PhysicsShape { get; set; }

    [JsonProperty("physicsShapePoints")]
    public List<object> PhysicsShapePoints { get; set; } = [];

    [JsonProperty("physicsStartAwake")]
    public bool PhysicsStartAwake { get; set; }

    [JsonProperty("properties")]
    public List<object> Properties { get; set; } = [];

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = string.Empty;

    [JsonProperty("solid")]
    public bool Solid { get; set; }

    [JsonProperty("spriteId")]
    public Reference? SpriteId { get; set; }

    [JsonProperty("spriteMaskId")]
    public Reference? SpriteMaskId { get; set; }

    [JsonProperty("visible")]
    public bool Visible { get; set; }
}