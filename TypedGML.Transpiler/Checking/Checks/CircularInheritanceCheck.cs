using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 2 (first): Detects circular inheritance chains (A extends B extends A).
///     Must run before any check that traverses the inheritance graph.
/// </summary>
public sealed class CircularInheritanceCheck : IAtomicCheck
{
    public string Name => "CircularInheritanceCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            Dfs(context, file, type, visited, new HashSet<string>(StringComparer.Ordinal));
    }

    private static void Dfs(TranspileContext ctx, TgmlFile file, TgmlTypeDecl type,
        HashSet<string> globalVisited, HashSet<string> path)
    {
        var key = type.QualifiedName ?? type.Name;

        if (!path.Add(key))
        {
            ctx.AddError($"Circular inheritance detected involving '{key}'.", file.FileName);
            return;
        }

        if (!globalVisited.Add(key))
        {
            path.Remove(key);
            return;
        }

        var bases = BaseRefs(type);
        foreach (var baseRef in bases)
        {
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                Dfs(ctx, file, baseDecl, globalVisited, path);
        }

        path.Remove(key);
    }

    private static IEnumerable<TgmlTypeRef> BaseRefs(TgmlTypeDecl type) =>
        type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            TgmlInterfaceDecl iface => iface.BaseInterfaces,
            _ => []
        };
}

