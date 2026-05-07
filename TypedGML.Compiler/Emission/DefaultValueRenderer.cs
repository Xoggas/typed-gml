using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

internal static class DefaultValueRenderer
{
    public static string Render(DefaultExpressionNode expression, EmitContext ctx)
    {
        if (ctx.CurrentType?.GenericParameters.Any(p => p.Name == expression.TypeName) == true)
            return $"__tgml_default(__genericArgs.{expression.TypeName})";

        if (expression.TypeName.EndsWith("?", StringComparison.Ordinal) ||
            expression.TypeName.EndsWith("[]", StringComparison.Ordinal))
            return "undefined";

        if (!ExpressionSymbolHelper.TryResolveType(ctx, expression.TypeName, out var type))
            return Fallback(expression.TypeName);

        return type.Kind switch
        {
            TypeKind.Struct => $"{NamingConvention.ConstructorName(type)}()",
            TypeKind.Enum => "0",
            _ => Fallback(type.QualifiedName)
        };
    }

    private static string Fallback(string typeName) => typeName switch
    {
        "number" => "0",
        "bool" => "false",
        "string" => "\"\"",
        _ => "undefined"
    };
}
