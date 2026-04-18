namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits a single __TypeMacros.gml file containing:
///     <list type="bullet">
///         <item>Built-in primitive type ID macros (negative IDs, always present)</item>
///         <item>User-defined type ID macros (<c>#macro __TYPE_X N</c>, N starting at 1)</item>
///     </list>
/// </summary>
public sealed class TypeMacroEmitter
{
    /// <summary>
    ///     Built-in primitive types that have no user declaration but may be used as
    ///     generic type arguments or in <c>typeof()</c> expressions.
    ///     Negative IDs avoid clashing with user-type IDs which start at 1.
    /// </summary>
    private static readonly (string Name, int Id)[] BuiltinTypes =
    [
        ("void",       0),
        ("int",       -1),
        ("real",      -2),
        ("string",    -3),
        ("bool",      -4),
        ("any",       -5),
        ("array",     -6),
        ("struct",    -7),
        ("object",    -8),
        ("undefined", -9)
    ];

    public GeneratedFile Emit(TranspileContext ctx)
    {
        var w = new GmlWriter();
        w.WriteLine("// Auto-generated type ID macros — do not edit manually.");
        w.WriteLine();

        w.WriteLine("// ── Built-in primitive types ─────────────────────────────────────────────");
        foreach (var (name, id) in BuiltinTypes)
            w.WriteLine($"#macro __TYPE_{name} {id}");

        w.WriteLine();
        w.WriteLine("// ── User-defined types ───────────────────────────────────────────────────");
        foreach (var (name, id) in ctx.TypeTable.AllTypeIds().OrderBy(t => t.Id))
        {
            var gmlName = name.Replace(".", "_");
            w.WriteLine($"#macro __TYPE_{gmlName} {id}");
        }

        return new GeneratedFile("Scripts/__TypeMacros/__TypeMacros.gml", w.ToString());
    }
}