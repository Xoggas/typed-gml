using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Emission;

public sealed class DecoratorProcessor
{
    public DecoratorAnnotations Process(IReadOnlyList<DecoratorNode> decorators, DiagnosticBag diag)
    {
        string? objectAsset = null;
        string? nativeEvent = null;
        string? nativeProperty = null;
        string? nativeCall = null;
        string? asset = null;

        foreach (var decorator in decorators)
        {
            var value = decorator.Args.FirstOrDefault() as LiteralExpressionNode;
            var text = value?.Value?.ToString();
            switch (decorator.Name)
            {
                case "Object":
                    objectAsset = text;
                    break;
                case "NativeEvent":
                    nativeEvent = text;
                    break;
                case "NativeProperty":
                    nativeProperty = text;
                    break;
                case "NativeCall":
                    nativeCall = text;
                    break;
                case "Asset":
                    asset = text;
                    break;
            }
        }

        return new DecoratorAnnotations(objectAsset, nativeEvent, nativeProperty, nativeCall, asset);
    }
}
