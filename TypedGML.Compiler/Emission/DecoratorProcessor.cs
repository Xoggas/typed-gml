using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

public sealed class DecoratorProcessor
{
    public DecoratorAnnotations Process(
        IReadOnlyList<DecoratorNode> decorators,
        DiagnosticBag diag,
        SymbolTable? symbols = null,
        TypeSymbol? currentType = null,
        string? currentNamespace = null,
        IReadOnlyList<string>? usingPrefixes = null)
    {
        string? objectAsset = null;
        string? nativeEvent = null;
        string? nativeProperty = null;
        string? nativeCall = null;
        string? asset = null;
        string? collisionTarget = null;

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
                case "Collision":
                    collisionTarget = ResolveCollisionTarget(
                        decorator,
                        diag,
                        symbols,
                        currentType,
                        currentNamespace,
                        usingPrefixes ?? []);
                    break;
            }
        }

        return new DecoratorAnnotations(objectAsset, nativeEvent, nativeProperty, nativeCall, asset, collisionTarget);
    }

    private static string? ResolveCollisionTarget(
        DecoratorNode decorator,
        DiagnosticBag diag,
        SymbolTable? symbols,
        TypeSymbol? currentType,
        string? currentNamespace,
        IReadOnlyList<string> usingPrefixes)
    {
        if (decorator.Args.FirstOrDefault() is not TypeofExpressionNode typeOf || symbols is null)
            return null;

        var namespaceName = string.IsNullOrEmpty(currentNamespace) ? CurrentNamespace(currentType) : currentNamespace;
        if (!symbols.TryResolve(typeOf.TypeName, namespaceName, usingPrefixes, out var targetType))
        {
            diag.Report(
                DiagnosticCode.TypeMismatch,
                DiagnosticSeverity.Error,
                $"Unknown type '{typeOf.TypeName}'.",
                typeOf.Location);
            return null;
        }

        return targetType.Kind != TypeKind.Class || string.IsNullOrEmpty(targetType.ObjectAssetName)
            ? null
            : $"Collision_{targetType.ObjectAssetName}";
    }

    private static string CurrentNamespace(TypeSymbol? currentType)
    {
        if (currentType is null)
            return string.Empty;

        var index = currentType.QualifiedName.LastIndexOf('.');
        return index < 0 ? string.Empty : currentType.QualifiedName[..index];
    }
}
