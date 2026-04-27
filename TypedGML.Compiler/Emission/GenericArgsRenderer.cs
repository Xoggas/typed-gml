using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

internal static class GenericArgsRenderer
{
    public static string Render(TypeSymbol type, IReadOnlyList<string> typeArgs, EmitContext ctx)
    {
        var pairs = type.GenericParameters.Zip(typeArgs)
            .Select(pair => $"{pair.First.Name}: \"{ResolveTypeName(pair.Second, ctx)}\"");
        return "{ " + string.Join(", ", pairs) + " }";
    }

    private static string ResolveTypeName(string typeRef, EmitContext ctx) =>
        ctx.Symbols.TryResolve(TypeRoot(typeRef), ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type)
            ? NamingConvention.TypeName(type)
            : typeRef;

    private static string TypeRoot(string value)
    {
        var stop = value.IndexOfAny(['<', '?', '[']);
        return stop >= 0 ? value[..stop] : value;
    }
}
