namespace TypedGML.Compiler.Symbols;

public static class PrimitiveBclTypeResolver
{
    public static TypeSymbol ResolveMemberOwner(TypeSymbol type, SymbolTable symbols) =>
        TryResolve(type, symbols, out var bclType) ? bclType : type;

    public static bool TryResolve(TypeSymbol type, SymbolTable symbols, out TypeSymbol bclType)
    {
        if (type.BclTypeName is null)
        {
            bclType = null!;
            return false;
        }

        if (symbols.TryResolve($"TypedGML.Core.{type.BclTypeName}", null, [], out bclType))
            return true;

        return symbols.TryResolve(type.BclTypeName, null, [], out bclType);
    }
}
