namespace TypedGML.Transpiler.GameMaker;

public sealed class FolderEntry : IEntry, IEquatable<FolderEntry>
{
    public string Path { get; }

    public FolderEntry(string path)
    {
        Path = path;
    }

    public bool Equals(FolderEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Path == other.Path;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is FolderEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }
}