using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class GenericParameterBinder(SymbolTable symbolTable, DiagnosticBag diagnostics)
{
    internal SymbolTable SymbolTable => symbolTable;

    internal DiagnosticBag Diagnostics => diagnostics;

    public void Populate(IReadOnlyList<Ast.FileNode> files)
    {
        foreach (var type in symbolTable.AllTypes)
        {
            var genericNames = type.GenericParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var member in type.Members)
            {
                member.ReturnType = Normalize(member.ReturnType, genericNames);
                member.Parameters = member.Parameters
                    .Select(p => p with { TypeRef = Normalize(p.TypeRef, genericNames) })
                    .ToList();
            }
        }
    }

    private string Normalize(string typeRef, IReadOnlySet<string> genericNames)
    {
        if (string.IsNullOrWhiteSpace(typeRef))
            return typeRef;

        var root = RootName(typeRef);
        if (genericNames.Contains(root))
            return typeRef;

        return typeRef;
    }

    private static string RootName(string typeRef)
    {
        var stop = typeRef.IndexOfAny(['<', '?', '[']);
        return stop >= 0 ? typeRef[..stop] : typeRef;
    }
}
