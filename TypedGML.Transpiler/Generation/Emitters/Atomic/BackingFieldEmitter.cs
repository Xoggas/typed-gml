using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits <c>__backing_PropertyName = default;</c> initializer statements for every
///     property that requires a compiler-generated backing field.
/// </summary>
/// <remarks>
///     A backing field is required when an auto accessor (<c>get;</c> / <c>set;</c>) is
///     present and the property is not mapped to a native GML property via
///     <c>@NativeProperty</c>.  Asset-typed properties are always skipped.
/// </remarks>
internal static class BackingFieldEmitter
{
    /// <summary>
    ///     Emits backing-field initialisers for <paramref name="properties"/> that need one.
    ///     The default value is always <c>undefined</c> for reference types and the
    ///     appropriate zero value for primitives.
    /// </summary>
    /// <param name="properties">The property list to inspect.</param>
    /// <param name="exprEmit">Expression emitter used to render default values.</param>
    /// <param name="ctx">Generation context (used to look up native property names).</param>
    /// <param name="w">Target GML writer.</param>
    /// <param name="useTypeDefault">
    ///     When <c>true</c>, the type's default value is used instead of <c>undefined</c>.
    ///     Pass <c>true</c> for structs/value types; <c>false</c> for class/object properties.
    /// </param>
    public static void Emit(
        IReadOnlyList<TgmlPropertyDecl> properties,
        ExpressionEmitter exprEmit,
        GenerationContext ctx,
        GmlWriter w,
        bool useTypeDefault = false)
    {
        foreach (var prop in properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _))
                continue;

            if (!EmitHelpers.RequiresBackingField(prop, ctx))
                continue;

            var value = useTypeDefault
                ? EmitHelpers.DefaultStorageValue(prop.Type, exprEmit)
                : "undefined";

            w.WriteLine($"__backing_{prop.Name} = {value};");
        }
    }
}

