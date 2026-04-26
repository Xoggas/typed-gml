namespace TypedGML.Compiler.Symbols;

public static class BuiltinTypeRegistry
{
    public static void RegisterInto(SymbolTable table)
    {
        table.Register("number", new TypeSymbol { QualifiedName = "number", Kind = TypeKind.Primitive });
        table.Register("string", new TypeSymbol { QualifiedName = "string", Kind = TypeKind.Primitive });
        table.Register("bool", new TypeSymbol { QualifiedName = "bool", Kind = TypeKind.Primitive });
        table.Register("void", new TypeSymbol { QualifiedName = "void", Kind = TypeKind.Primitive });
        table.Register("null", new TypeSymbol { QualifiedName = "null", Kind = TypeKind.Primitive });

        var objectType = new TypeSymbol { QualifiedName = "object", Kind = TypeKind.Primitive };
        objectType.Members.Add(new MemberSymbol
        {
            Name = "ToString",
            Kind = MemberKind.Method,
            ReturnType = "string"
        });
        objectType.Members.Add(new MemberSymbol
        {
            Name = "GetType",
            Kind = MemberKind.Method,
            ReturnType = "string"
        });

        table.Register("object", objectType);
    }
}
