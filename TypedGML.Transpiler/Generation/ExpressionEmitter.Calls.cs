using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private string EmitMethodCall(TgmlMethodCallExpr e)
    {
        if (e.Metadata.ContainsKey(DelegateInvokeTargetMetadata))
        {
            var targetExpr = new TgmlFieldAccessExpr { Target = e.Target, FieldName = e.MethodName };
            return EmitDelegateInvoke(targetExpr, e, GetNormalizedArgs(e, e.Args));
        }

        var target = Emit(e.Target);
        var normalizedArgs = GetNormalizedArgs(e, e.Args);

        if (TryEmitInlinedPrimitiveMethodCall(e, normalizedArgs, out var inlinedPrimitiveMethodCall))
            return inlinedPrimitiveMethodCall;

        if (e.Metadata.TryGetValue("NativeInstanceCallName", out var nativeCall) && nativeCall is string nativeCallName)
            return $"{nativeCallName}({string.Join(", ", new[] { target }.Concat(normalizedArgs.Select(Emit)))})";

        var args = string.Join(", ", normalizedArgs.Select(Emit));
        if (_ctx.WithAlias is { } alias && target == alias)
            return $"{e.MethodName}({args})";

        return $"{target}.{e.MethodName}({args})";
    }

    private string EmitFuncCall(TgmlFuncCallExpr e)
    {
        if (e.Metadata.ContainsKey(DelegateInvokeTargetMetadata))
            return EmitDelegateInvoke(new TgmlIdExpr { Name = e.FunctionName }, e, GetNormalizedArgs(e, e.Args));

        return $"{e.FunctionName}({string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit))})";
    }

    private string EmitFieldAccess(TgmlFieldAccessExpr e)
    {
        if (e.Metadata.TryGetValue(AssetReferenceNameMetadata, out var assetReference) && assetReference is string assetName)
            return assetName;
        if (e.Metadata.TryGetValue(EnumFacts.EnumMemberMacroMetadata, out var enumMacro) && enumMacro is string enumMacroName)
            return enumMacroName;
        if (_ctx.TryResolveStaticAssetReference(e.Target, e.FieldName, out var resolvedAssetName))
            return resolvedAssetName;
        if (TryEmitNativePropertyAccess(e.Target, e.FieldName, out var nativeAccess))
            return nativeAccess;
        if (TryEmitPropertyAccess(e.Target, e.FieldName, out var propertyAccess))
            return propertyAccess;

        var target = Emit(e.Target);
        if (_ctx.WithAlias is { } alias && target == alias)
            return e.FieldName;

        return $"{target}.{e.FieldName}";
    }

    private string EmitIndex(TgmlIndexExpr e)
    {
        if (TryEmitIndexerAccess(e, out var indexerAccess))
            return indexerAccess;

        return $"{Emit(e.Target)}[{Emit(e.Index)}]";
    }
}
