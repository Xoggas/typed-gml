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
        ctx.Symbols.TryResolve(typeRef, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type)
            ? NamingConvention.TypeName(type)
            : typeRef;
}
