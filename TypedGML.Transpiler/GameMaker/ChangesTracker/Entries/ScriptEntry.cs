namespace TypedGML.Transpiler.GameMaker;

public sealed class ScriptEntry : IEntry, IEquatable<ScriptEntry>
{
    public string Name { get; }

    public ScriptEntry(string name)
    {
        Name = name;
    }

    public bool Equals(ScriptEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ScriptEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}