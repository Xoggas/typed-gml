namespace TypedGML.Transpiler.Population.Models;

/// <summary>A decorator application, e.g. @Object("obj_Player") or @NativeEvent("Step").</summary>
public sealed class TgmlDecorator
{
    public required TgmlQualifiedName Name { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();

    /// <summary>Simple (last-segment) decorator name, e.g. "Object".</summary>
    public string SimpleName => Name.Simple;

    /// <summary>Returns the first string literal arg value, or null.</summary>
    public string? GetFirstStringArg()
    {
        if (Args.Count > 0 && Args[0].Value is TgmlStringLiteralExpr s)
        {
            return s.Value;
        }

        return null;
    }
}
