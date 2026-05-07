using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class StaticAutoPropertyInitializerEmitter
{
    public void Emit(TypeSymbol type, IEnumerable<PropertyDeclarationNode> properties, EmitContext ctx)
    {
        foreach (var property in properties.Where(IsAutoProperty))
        {
            var symbol = type.Members.FirstOrDefault(m => m.Kind == MemberKind.Property && m.Name == property.Name);
            if (symbol is null)
                continue;

            var value = DefaultValueRenderer.Render(new DefaultExpressionNode(property.TypeRef, property.Location), ctx);
            ctx.Writer.WriteLine($"{NamingConvention.StaticPropertyBackingName(type, symbol)} = {value};");
        }
    }

    private static bool IsAutoProperty(PropertyDeclarationNode property) =>
        property.Accessors.Count > 0 &&
        property.Accessors.All(accessor => accessor.Body is null) &&
        DecoratorArg(property.Decorators, "NativeProperty") is null &&
        DecoratorArg(property.Decorators, "Asset") is null;

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
