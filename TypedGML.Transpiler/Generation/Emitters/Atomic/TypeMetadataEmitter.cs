using TypedGML.Transpiler.Generation.Helpers;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits the per-type GML metadata block:
///     <list type="bullet">
///         <item><c>__types = { __TYPE_X: true, … };</c></item>
///         <item><c>__genericArgs = [__t0, …];</c> (when the type has generic parameters)</item>
///         <item><c>ToString = function() { return "Qualified.Name"; };</c> (default, overridable)</item>
///         <item><c>GetType = function() { return __TYPE_X; };</c> (synthesised, always last)</item>
///     </list>
/// </summary>
/// <remarks>
///     <c>GetType</c> is emitted <b>last</b> so it cannot be accidentally replaced later in the
///     generated constructor body.
/// </remarks>
internal static class TypeMetadataEmitter
{
    /// <summary>
    ///     Emits the opening metadata block (<c>__types</c> and optional <c>__genericArgs</c>)
    ///     into <paramref name="w"/>.
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
    /// <param name="w">Target GML writer.</param>
    public static void EmitHeader(
        string typeIds,
        IReadOnlyList<string> typeArgParams,
        GmlWriter w)
    {
        w.WriteLine($"__types = {{ {typeIds} }};");

        if (typeArgParams.Count > 0)
            w.WriteLine($"__genericArgs = [{string.Join(", ", typeArgParams)}];");
    }

    public static void EmitDefaultToString(string qualifiedTypeName, GmlWriter w)
    {
        w.WriteLine();
        w.WriteLine($"ToString = function() {{ return \"{qualifiedTypeName}\"; }};");
    }

    /// <summary>
    ///     Emits the synthesised <c>GetType</c> function.
    ///     Must be called <b>last</b> inside the constructor body so it cannot be overridden.
    /// </summary>
    /// <param name="gmlName">GML-safe type name, e.g. <c>System_Collections_List</c>.</param>
    /// <param name="w">Target GML writer.</param>
    public static void EmitGetType(string gmlName, GmlWriter w)
    {
        w.WriteLine();
        w.WriteLine($"GetType = function() {{ return __TYPE_{gmlName}; }};");
    }
}

