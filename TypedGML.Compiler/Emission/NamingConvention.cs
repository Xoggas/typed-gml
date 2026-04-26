using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

public static class NamingConvention
{
    public static string TypeName(TypeSymbol type) =>
        type.QualifiedName.Replace('.', '_');

    public static string MethodName(TypeSymbol type, MemberSymbol method) =>
        $"{TypeName(type)}_{method.Name}{OverloadSuffix(method)}";

    public static string ConstructorName(TypeSymbol type) =>
        $"{TypeName(type)}_create";

    public static string PropertyGetter(TypeSymbol type, MemberSymbol prop) =>
        $"{TypeName(type)}_get_{prop.Name}";

    public static string PropertySetter(TypeSymbol type, MemberSymbol prop) =>
        $"{TypeName(type)}_set_{prop.Name}";

    public static string OperatorName(TypeSymbol type, string op) =>
        $"{TypeName(type)}_op_{OperatorPart(op)}";

    public static string ConstMacro(TypeSymbol type, MemberSymbol field) =>
        $"{TypeName(type)}_{field.Name}";

    public static string EnumMember(TypeSymbol enumType, string member) =>
        $"{TypeName(enumType)}_{member}";

    public static string GlobalProperty(MemberSymbol prop) =>
        $"global.{prop.Name}";

    private static string OverloadSuffix(MemberSymbol method) =>
        method.Overloads.Count > 0
            ? "__" + string.Join("_", method.Parameters.Select(p => p.TypeRef.Replace(".", "_", StringComparison.Ordinal)))
            : string.Empty;

    private static string OperatorPart(string op) => op switch
    {
        "+" => "add",
        "-" => "sub",
        "*" => "mul",
        "/" => "div",
        "%" => "mod",
        "==" => "eq",
        "!=" => "neq",
        "<" => "lt",
        ">" => "gt",
        "<=" => "le",
        ">=" => "ge",
        "~" => "bitnot",
        _ => op.Replace(".", "_", StringComparison.Ordinal)
    };
}
