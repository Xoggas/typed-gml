namespace TypedGML.Transpiler.Checking;

/// <summary>A named group of atomic checks that run together.</summary>
public sealed class CheckBatch
{
    public required string Name { get; init; }
    public List<IAtomicCheck> Checks { get; init; } = new();

    public CheckBatch Add(IAtomicCheck check)
    {
        Checks.Add(check);
        return this;
    }
}