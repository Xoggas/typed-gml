using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Shared helper for collecting the full transitive inheritance chain of a type
///     so that <c>__types</c> structs contain every ancestor (base classes + all interfaces).
/// </summary>
internal static class TypeHierarchyHelper
{
    /// <summary>
    ///     Returns the GML-style names (dots replaced by underscores) of <paramref name="decl" />
    ///     itself and every type in its full transitive inheritance hierarchy.
    ///     Cycles are guarded by <paramref name="visited" />.
    /// </summary>
    public static List<string> CollectAllGmlTypeNames(
        TgmlTypeDecl decl,
        TypeTable typeTable,
        HashSet<string>? visited = null)
    {
        visited ??= new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        Collect(decl, typeTable, result, visited);
        return result;
    }

    private static void Collect(
        TgmlTypeDecl decl,
        TypeTable typeTable,
        List<string> result,
        HashSet<string> visited)
    {
        var selfGml = decl.QualifiedName?.Replace(".", "_") ?? decl.Name;
        if (!visited.Add(selfGml))
            return;

        result.Add(selfGml);

        var bases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            TgmlInterfaceDecl i => i.BaseInterfaces,
            _ => null
        };

        if (bases is null)
            return;

        foreach (var bt in bases)
        {
            typeTable.TryResolve(bt.Name.Full, out var btDecl);

            if (btDecl is not null)
            {
                // Recurse — this also adds btDecl's own name as the first entry
                Collect(btDecl, typeTable, result, visited);
            }
            else
            {
                // Unresolvable base — add by raw GML name, no further recursion possible
                var rawGml = bt.GmlBaseName;
                if (visited.Add(rawGml))
                    result.Add(rawGml);
            }
        }
    }
}

