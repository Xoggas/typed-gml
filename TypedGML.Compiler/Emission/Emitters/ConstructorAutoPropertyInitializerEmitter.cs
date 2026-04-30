using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorAutoPropertyInitializerEmitter
{
    public static void Emit(TypeSymbol? type, EmitContext ctx)
    {
        if (type is null)
            return;

        foreach (var property in AutoProperties(type, ctx))
        {
            var target = NamingConvention.InstancePropertyBackingName("self", property.Name);
            var value = DefaultValueRenderer.Render(new DefaultExpressionNode(property.TypeRef, property.Location), ctx);
            ctx.Writer.WriteLine($"{target} = {value};");
        }
    }

    private static IEnumerable<PropertyDeclarationNode> AutoProperties(TypeSymbol type, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var declaration))
            return [];

        return declaration switch
        {
            ClassDeclarationNode @class => InstanceAutoProperties(@class.Members),
            StructDeclarationNode @struct => InstanceAutoProperties(@struct.Members),
            _ => []
        };
    }

    private static IEnumerable<PropertyDeclarationNode> InstanceAutoProperties(IEnumerable<IAstNode> members) =>
        members.OfType<PropertyDeclarationNode>().Where(IsInstanceAutoProperty);

    private static bool IsInstanceAutoProperty(PropertyDeclarationNode property) =>
        !property.Modifiers.Contains("static", StringComparer.Ordinal) &&
        !property.Modifiers.Contains("abstract", StringComparer.Ordinal) &&
        property.Accessors.Count > 0 &&
        property.Accessors.All(accessor => accessor.Body is null) &&
        DecoratorArg(property.Decorators, "NativeProperty") is null &&
        DecoratorArg(property.Decorators, "Asset") is null;

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
