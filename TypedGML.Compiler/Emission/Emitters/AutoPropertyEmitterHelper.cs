using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class AutoPropertyEmitterHelper
{
    public static bool IsCompilerBacked(PropertyDeclarationNode property) =>
        property.Accessors.Count > 0 &&
        property.Accessors.All(accessor => accessor.Body is null) &&
        DecoratorArg(property.Decorators, "NativeProperty") is null &&
        DecoratorArg(property.Decorators, "Asset") is null;

    public static string RenderInitialValue(PropertyDeclarationNode property, EmitContext ctx) =>
        property.Initializer is null
            ? DefaultValueRenderer.Render(new DefaultExpressionNode(property.TypeRef, property.Location), ctx)
            : ctx.RenderWithExpectedTempPrelude(property.Initializer, property.TypeRef);

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
