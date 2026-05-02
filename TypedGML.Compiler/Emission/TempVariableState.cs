namespace TypedGML.Compiler.Emission;

internal sealed class TempVariableState
{
    public int Counter { get; set; }

    public int PreludeDepth { get; set; }

    public int SuppressDepth { get; set; }

    public List<string> Prelude { get; } = [];
}
