using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Decorators;

/// <summary>
///     Handles @Object("obj_Name") — marks the class as a GameMaker object.
///     Stores "GmlObjectName" in the type's Metadata.
/// </summary>
public sealed class ObjectDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => "Object";

    public void ApplyToType(TgmlDecorator decorator, TgmlTypeDecl type, TranspileContext ctx)
    {
        var objName = decorator.GetFirstStringArg();
        if (objName is null)
        {
            ctx.AddError($"@Object on '{type.Name}' requires a string argument (the GML object name).");
            return;
        }

        type.Metadata["GmlObjectName"] = objName;
    }
}

/// <summary>
///     Handles @NativeEvent("EventName") — maps a method to a GameMaker event script.
///     Stores "NativeEventName" in the method's Metadata.
/// </summary>
public sealed class NativeEventDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => "NativeEvent";

    public void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
        var eventName = decorator.GetFirstStringArg();
        if (eventName is null)
        {
            ctx.AddError($"@NativeEvent on '{member.Name}' requires a string argument.");
            return;
        }

        member.Metadata["NativeEventName"] = eventName;
    }
}

/// <summary>
///     Handles @NativeProperty("gml_name") — maps a TypedGML property to a native GML variable.
///     Stores "NativePropertyName" in the property's Metadata.
/// </summary>
public sealed class NativePropertyDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => "NativeProperty";

    public void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
        var propName = decorator.GetFirstStringArg();
        if (propName is null)
        {
            ctx.AddError($"@NativeProperty on '{member.Name}' requires a string argument.");
            return;
        }

        member.Metadata["NativePropertyName"] = propName;
    }
}

/// <summary>
///     Handles @NativeInstanceCall("gml_func") — maps a TypedGML instance method to a GML function
///     where the calling instance is passed as the first argument: gml_func(instance, args...).
///     Used for primitive type extension-style methods (string, number, bool, array).
///     Stores "NativeInstanceCallName" in the method's Metadata.
/// </summary>
public sealed class NativeInstanceCallDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => "NativeInstanceCall";

    public void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
        var callName = decorator.GetFirstStringArg();
        if (callName is null)
        {
            ctx.AddError($"@NativeInstanceCall on '{member.Name}' requires a string argument.");
            return;
        }

        member.Metadata["NativeInstanceCallName"] = callName;
    }
}

/// <summary>
///     Handles @NativeCall("gml_function_name") — maps a TypedGML method to a native GML function call.
///     Stores "NativeCallName" in the method's Metadata.
/// </summary>
public sealed class NativeCallDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => "NativeCall";

    public void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
        var callName = decorator.GetFirstStringArg();
        if (callName is null)
        {
            ctx.AddError($"@NativeCall on '{member.Name}' requires a string argument.");
            return;
        }

        member.Metadata["NativeCallName"] = callName;
    }
}

/// <summary>
///     Handles <c>@Asset("asset_name")</c> — binds a static registry member to a raw GML asset identifier.
///     Stores "AssetName" in the member's Metadata.
/// </summary>
public sealed class AssetDecoratorHandler : IDecoratorHandler
{
    public string DecoratorName => AssetFacts.DecoratorName;

    public void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
        var assetName = decorator.GetFirstStringArg();
        if (assetName is null)
        {
            ctx.AddError($"@Asset on '{member.Name}' requires a string argument.");
            return;
        }

        member.Metadata[AssetFacts.AssetNameMetadata] = assetName;
    }
}
