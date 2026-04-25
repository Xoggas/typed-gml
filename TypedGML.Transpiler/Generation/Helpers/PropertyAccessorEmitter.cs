using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Helpers;

/// <summary>
///     Shared property/indexer accessor emitter used by both
///     <c>ScriptClassEmitter</c> (which needs <c>static</c> GML functions) and
///     <c>GameObjectEmitter</c> (which uses plain instance assignments).
/// </summary>
internal static class PropertyAccessorEmitter
{
    /// <summary>
    ///     Emits getter and setter GML functions for <paramref name="prop"/>.
    ///     Pass <paramref name="isStatic"/><c>=true</c> for script classes (GML <c>static</c> keyword)
    ///     and <c>false</c> for GameMaker object Create events.
    /// </summary>
    public static void EmitProperty(
        TgmlPropertyDecl prop,
        GenerationContext ctx,
        GmlWriter w,
        bool isStatic)
    {
        var stmtEmit = new StatementEmitter(ctx);

        if (prop.IsIndexer)
        {
            EmitIndexer(prop, ctx, stmtEmit, w, isStatic);
            return;
        }

        var nativePropertyName = ctx.GetNativePropertyName(prop);
        var prefix = isStatic ? "static " : string.Empty;
        ctx.CurrentPropertyName = prop.Name;

        if (AssetFacts.TryGetAssetName(prop, out var assetName))
        {
            if (prop.Getter is not null)
                w.WriteLine($"{prefix}get_{prop.Name} = function() {{ return {assetName}; }}{(isStatic ? ";" : string.Empty)}");

            ctx.InsideGetter = false;
            ctx.InsideSetter = false;
            ctx.CurrentPropertyName = null;
            return;
        }

        if (prop.Getter is { } getter)
        {
            ctx.InsideGetter = true;
            ctx.InsideSetter = false;

            if (nativePropertyName is not null && getter.IsAuto)
                w.WriteLine($"{prefix}get_{prop.Name} = function() {{ return {nativePropertyName}; }}{(isStatic ? ";" : string.Empty)}");
            else if (getter.IsAuto)
                w.WriteLine($"{prefix}get_{prop.Name} = function() {{ return __backing_{prop.Name}; }}{(isStatic ? ";" : string.Empty)}");
            else
            {
                w.WriteLine($"{prefix}get_{prop.Name} = function()");
                w.OpenBrace();
                if (getter.Body is not null) stmtEmit.EmitBlock(getter.Body, w);
                w.CloseBrace();
            }
        }

        if (prop.Setter is { } setter)
        {
            ctx.InsideGetter = false;
            ctx.InsideSetter = true;

            if (nativePropertyName is not null && setter.IsAuto)
                w.WriteLine($"{prefix}set_{prop.Name} = function(value) {{ {nativePropertyName} = value; }}{(isStatic ? ";" : string.Empty)}");
            else if (setter.IsAuto)
                w.WriteLine($"{prefix}set_{prop.Name} = function(value) {{ __backing_{prop.Name} = value; }}{(isStatic ? ";" : string.Empty)}");
            else
            {
                w.WriteLine($"{prefix}set_{prop.Name} = function(value)");
                w.OpenBrace();
                if (setter.Body is not null) stmtEmit.EmitBlock(setter.Body, w);
                w.CloseBrace();
            }
        }

        ctx.InsideGetter = false;
        ctx.InsideSetter = false;
        ctx.CurrentPropertyName = null;
    }

    /// <summary>
    ///     Emits <c>get_Item</c> / <c>set_Item</c> GML functions for an indexer property.
    /// </summary>
    private static void EmitIndexer(
        TgmlPropertyDecl prop,
        GenerationContext ctx,
        StatementEmitter stmtEmit,
        GmlWriter w,
        bool isStatic)
    {
        var indexParamName = prop.IndexParam?.Name ?? "index";
        var prefix = isStatic ? "static " : string.Empty;
        ctx.CurrentPropertyName = null;

        if (prop.Getter is { } getter)
        {
            ctx.InsideGetter = true;
            ctx.InsideSetter = false;
            w.WriteLine($"{prefix}get_Item = function({indexParamName})");
            w.OpenBrace();
            if (getter.Body is not null) stmtEmit.EmitBlock(getter.Body, w);
            w.CloseBrace();
        }

        if (prop.Setter is { } setter)
        {
            ctx.InsideGetter = false;
            ctx.InsideSetter = true;
            w.WriteLine($"{prefix}set_Item = function({indexParamName}, value)");
            w.OpenBrace();
            if (setter.Body is not null) stmtEmit.EmitBlock(setter.Body, w);
            w.CloseBrace();
        }

        ctx.InsideGetter = false;
        ctx.InsideSetter = false;
        ctx.CurrentPropertyName = null;
    }
}

