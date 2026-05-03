namespace TypedGML.Compiler.Verification;

internal readonly record struct AssignmentNarrowing(string Name, string TypeRef)
{
    public static AssignmentNarrowing None { get; } = new(string.Empty, string.Empty);

    public bool HasValue => !string.IsNullOrEmpty(Name);
}
