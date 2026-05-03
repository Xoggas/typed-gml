using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Utils;

public static class TypeHelper
{
    public static bool IsNullable(string typeRef) =>
        !string.IsNullOrEmpty(typeRef) && typeRef.EndsWith("?", StringComparison.Ordinal);

    public static string UnwrapNullable(string typeRef) =>
        IsNullable(typeRef) ? typeRef[..^1] : typeRef;

    public static bool IsAssignableTo(TypeSymbol from, TypeSymbol to, SymbolTable symbols)
    {
        if (from == to || to.QualifiedName == "object")
            return true;

        for (var current = from.Base; current is not null; current = current.Base)
            if (current == to)
                return true;

        return from.Interfaces.Any(@interface => @interface == to);
    }

    public static bool IsVoid(string typeRef) =>
        string.Equals(typeRef, "void", StringComparison.Ordinal);

    public static bool IsPrimitive(TypeSymbol symbol) =>
        symbol.Kind == TypeKind.Primitive;
}
