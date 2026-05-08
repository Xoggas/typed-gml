using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class PropertyInitializerCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is PropertyDeclarationNode { Initializer: not null };

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var property = (PropertyDeclarationNode)node;
        if (!IsInitializableAutoProperty(property))
        {
            Report(DiagnosticCode.TypeMismatch, "Only compiler-backed auto-properties can have initializer expressions.", property.Location, ctx);
            return;
        }

        if (property.Initializer is DictionaryLiteralExpressionNode)
        {
            if (TypeReferenceHelper.RootName(property.TypeRef) != "Dictionary")
                Report(DiagnosticCode.InvalidDictionaryLiteralTarget, "Dictionary literals are only valid when the target type is Dictionary<K,V>.", property.Location, ctx);
            return;
        }

        if (property.Initializer is LambdaExpressionNode &&
            DelegateTypeHelper.TrySignature(property.TypeRef, ctx, out _, out _))
            return;

        var initializerType = ExpressionTypeResolver.Resolve(property.Initializer, ctx);
        if (IsArrayLiteralForTarget(property.Initializer, property.TypeRef, ctx))
            return;

        if (!TypeReferenceHelper.IsAssignable(property.TypeRef, initializerType, ctx))
            Report(DiagnosticCode.TypeMismatch, $"Cannot assign '{initializerType ?? "unknown"}' to '{property.TypeRef}'.", property.Location, ctx);
    }

    private static bool IsInitializableAutoProperty(PropertyDeclarationNode property) =>
        property.Accessors.Count > 0 &&
        property.Accessors.All(accessor => accessor.Body is null) &&
        DecoratorArg(property.Decorators, "NativeProperty") is null &&
        DecoratorArg(property.Decorators, "Asset") is null;

    private static bool IsArrayLiteralForTarget(IAstNode? value, string? targetType, VerificationContext ctx) =>
        value is ArrayLiteralExpressionNode && ArrayLiteralTargetHelper.IsLiteralTarget(targetType, ctx);

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
