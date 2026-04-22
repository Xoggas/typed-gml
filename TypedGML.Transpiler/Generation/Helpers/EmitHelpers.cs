using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Helpers;

/// <summary>
///     Shared emit helpers used by multiple emitter classes.
///     Centralises decisions about backing fields and the <c>__types</c> struct.
/// </summary>
internal static class EmitHelpers
{
    /// <summary>
    ///     Returns <c>true</c> when <paramref name="prop"/> needs a compiler-generated
    ///     <c>__backing_{Name}</c> field (auto accessor, not a native property, not an indexer).
    /// </summary>
    public static bool RequiresBackingField(TgmlPropertyDecl prop, GenerationContext ctx)
    {
        if (prop.IsIndexer)
            return false;

        if (AssetFacts.TryGetAssetName(prop, out _))
            return false;

        return (prop.Getter?.IsAuto == true || prop.Setter?.IsAuto == true) &&
               ctx.GetNativePropertyName(prop) is null;
    }

    /// <summary>
    ///     Builds the GML struct literal assigned to <c>__types</c>, e.g.
    ///     <c>__TYPE_Foo: true, __TYPE_IBar: true</c>.
    ///     Every type in the full transitive inheritance chain is included.
    /// </summary>
    public static string BuildTypesStruct(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var names = TypeHierarchyHelper.CollectAllGmlTypeNames(decl, ctx.TypeTable);
        return string.Join(", ", names.Select(n => $"__TYPE_{n}: true"));
    }

    /// <summary>
    ///     Returns the GML expression for the default/zero value of <paramref name="type"/>,
    ///     delegating to <see cref="ExpressionEmitter"/> for struct defaults.
    /// </summary>
    public static string DefaultStorageValue(
        TgmlTypeRef type,
        ExpressionEmitter exprEmit)
        => exprEmit.Emit(new TgmlDefaultExpr { Type = type });
}

