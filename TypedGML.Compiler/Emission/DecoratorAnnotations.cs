namespace TypedGML.Compiler.Emission;

public sealed record DecoratorAnnotations(
    string? ObjectAssetName,
    string? NativeEventName,
    string? NativePropertyName,
    string? NativeCallName,
    string? AssetName);
