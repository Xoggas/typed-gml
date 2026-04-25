namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits a single __TypeMacros.gml file containing:
///     <list type="bullet">
///         <item>Built-in primitive type ID macros (stringified IDs, always present)</item>
///         <item>User-defined type ID macros (<c>#macro __TYPE_X "N"</c>, N starting at 1)</item>
///     </list>
/// </summary>
public sealed class TypeMacroEmitter
{
    /// <summary>
    ///     Built-in primitive types that have no user declaration but may be used as
    ///     generic type arguments or in <c>typeof()</c> expressions.
///     IDs remain numeric inside the compiler, but are emitted as strings for GameMaker
///     runtime metadata so they can be safely used as struct keys in <c>__types</c>.
    /// </summary>
    private static readonly (string Name, int Id)[] BuiltinTypes =
    [
        ("void",       0),
        ("number",    -1),
        ("int",       -1),
        ("real",      -1),
        ("long",      -1),
        ("string",    -2),
        ("bool",      -3),
        ("any",       -4),
        ("array",     -5),
        ("struct",    -6),
        ("object",    -7),
        ("undefined", -8)
    ];

    public GeneratedFile Emit(TranspileContext ctx)
    {
        var w = new GmlWriter();
        w.WriteLine("// Auto-generated type ID macros — do not edit manually.");
        w.WriteLine();

        w.WriteLine("// ── Built-in primitive types ─────────────────────────────────────────────");
        foreach (var (name, id) in BuiltinTypes)
            w.WriteLine($"#macro __TYPE_{name} \"{id}\"");

        w.WriteLine();
        w.WriteLine("// ── User-defined types ───────────────────────────────────────────────────");
        foreach (var decl in ctx.TypeTable.All.Where(t => t is not TypedGML.Transpiler.Population.Models.TgmlDelegateDecl))
        {
            var id = ctx.TypeTable.GetTypeId(decl);
            var gmlName = (decl.QualifiedName ?? decl.Name).Replace(".", "_");
            w.WriteLine($"#macro __TYPE_{gmlName} \"{id}\"");
        }

        return new GeneratedFile("Scripts/__TypeMacros/__TypeMacros.gml", w.ToString());
    }
}
