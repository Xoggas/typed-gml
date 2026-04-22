using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits GML field initializer statements for a list of <see cref="TgmlFieldDecl"/>s.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>Instance fields → <c>self.FieldName = defaultOrInit;</c></item>
///         <item>Static fields → <c>global.OwnerGmlName_FieldName = defaultOrInit;</c></item>
///         <item>Asset-typed fields are skipped (they carry no GML runtime value).</item>
///     </list>
/// </remarks>
internal static class FieldInitEmitter
{
    /// <summary>
    ///     Emits initializer statements for all fields in <paramref name="fields"/>.
    /// </summary>
    /// <param name="fields">Fields to emit. Asset fields are silently skipped.</param>
    /// <param name="ownerGmlName">
    ///     GML-safe name of the declaring type, used as a prefix for static fields:
    ///     <c>global.{ownerGmlName}_{FieldName}</c>.
    /// </param>
    /// <param name="exprEmit">Expression emitter used to render default/initializer values.</param>
    /// <param name="w">Target GML writer.</param>
    public static void Emit(
        IReadOnlyList<TgmlFieldDecl> fields,
        string ownerGmlName,
        ExpressionEmitter exprEmit,
        GmlWriter w)
    {
        foreach (var field in fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _))
                continue;

            var value = field.Initializer is not null
                ? exprEmit.Emit(field.Initializer)
                : EmitHelpers.DefaultStorageValue(field.Type, exprEmit);

            if (field.IsStatic)
                w.WriteLine($"global.{ownerGmlName}_{field.Name} = {value};");
            else
                w.WriteLine($"self.{field.Name} = {value};");
        }
    }

    /// <summary>
    ///     Variant used inside GameMaker object Create events, where instance fields
    ///     are written without a <c>self.</c> prefix (GML instance scope is implicit).
    /// </summary>
    public static void EmitForGameObject(
        IReadOnlyList<TgmlFieldDecl> fields,
        string ownerGmlName,
        ExpressionEmitter exprEmit,
        GmlWriter w)
    {
        foreach (var field in fields)
        {
            if (AssetFacts.TryGetAssetName(field, out _))
                continue;

            var value = field.Initializer is not null
                ? exprEmit.Emit(field.Initializer)
                : exprEmit.Emit(new TgmlDefaultExpr { Type = field.Type });

            if (field.IsStatic)
                w.WriteLine($"global.{ownerGmlName}_{field.Name} = {value};");
            else
                w.WriteLine($"{field.Name} = {value};");
        }
    }
}

