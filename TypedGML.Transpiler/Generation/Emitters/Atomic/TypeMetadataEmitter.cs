using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits the per-type GML metadata block:
///     <list type="bullet">
///         <item><c>__types = { __TYPE_X: true, … };</c></item>
///         <item><c>__genericArgs = [__t0, …];</c> (when the type has generic parameters)</item>
///         <item><c>static ToString = function() { return "Qualified.Name"; };</c> (default, overridable)</item>
///         <item><c>static GetType = function() { return __TYPE_X; };</c> (synthesised, always last)</item>
///     </list>
/// </summary>
/// <remarks>
///     <c>ToString</c> is intentionally emitted <b>first</b> so that any user-defined or
///     ancestor <c>ToString</c> emitted afterwards will overwrite it (GML last-writer-wins).
///     <c>GetType</c> is emitted <b>last</b> so it cannot be overridden.
/// </remarks>
internal static class TypeMetadataEmitter
{
    /// <summary>
    ///     Emits the opening metadata block (<c>__types</c>, optional <c>__genericArgs</c>,
    ///     and the default <c>ToString</c>) into <paramref name="w"/>.
    ///     Call this immediately after opening the GML constructor brace.
    /// </summary>
    /// <param name="typeIds">
    ///     Pre-built types-struct body string, e.g. <c>__TYPE_Foo: true, __TYPE_IBar: true</c>.
    ///     Obtain from <see cref="EmitHelpers.BuildTypesStruct"/>.
    /// </param>
    /// <param name="typeArgParams">
    ///     GML parameter names for generic type arguments (<c>__t0</c>, <c>__t1</c>, …).
    ///     Pass an empty list for non-generic types.
    /// </param>
    /// <param name="qualifiedTypeName">The fully-qualified TypedGML name returned by <c>ToString()</c>.</param>
    public static void EmitHeader(
        string typeIds,
        IReadOnlyList<string> typeArgParams,
        string qualifiedTypeName,
        GmlWriter w)
    {
        w.WriteLine($"__types = {{ {typeIds} }};");

        if (typeArgParams.Count > 0)
            w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParams)}];");

        // Emit default ToString first — user / ancestor overrides written later will win.
        w.WriteLine($"static ToString = function() {{ return \"{qualifiedTypeName}\"; }};");
    }

    /// <summary>
    ///     Emits the synthesised <c>static GetType</c> function.
    ///     Must be called <b>last</b> inside the constructor body so it cannot be overridden.
    /// </summary>
    /// <param name="gmlName">GML-safe type name, e.g. <c>System_Collections_List</c>.</param>
    public static void EmitGetType(string gmlName, GmlWriter w)
    {
        w.WriteLine();
        w.WriteLine($"static GetType = function() {{ return __TYPE_{gmlName}; }};");
    }
}

