namespace TypedGML.Compiler.Symbols;

internal static class ExceptionNavigation
{
    public static bool IsExceptionType(TypeSymbol? type) =>
        IsExceptionTypeRef(type?.QualifiedName);

    public static bool IsExceptionTypeRef(string? typeRef)
    {
        var root = RootName(typeRef);
        return string.Equals(root, "Exception", StringComparison.Ordinal) ||
               string.Equals(root, "TypedGML.Core.Exception", StringComparison.Ordinal);
    }

    public static bool TryResolveExceptionType(
        SymbolTable symbols,
        string? currentNamespace,
        IReadOnlyList<string> usingPrefixes,
        out TypeSymbol symbol) =>
        symbols.TryResolve("Exception", currentNamespace, usingPrefixes, out symbol) ||
        symbols.TryResolve("TypedGML.Core.Exception", null, [], out symbol);

    public static bool TryGetNativeField(string memberName, out string fieldName)
    {
        fieldName = memberName switch
        {
            "Message" or "message" => "message",
            "LongMessage" or "longMessage" => "longMessage",
            "Script" or "script" => "script",
            "Stacktrace" or "stacktrace" => "stacktrace",
            _ => string.Empty
        };
        return fieldName.Length > 0;
    }

    private static string RootName(string? typeRef)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return string.Empty;

        var stop = typeRef.IndexOfAny(['<', '?', '[']);
        return stop >= 0 ? typeRef[..stop] : typeRef;
    }
}
