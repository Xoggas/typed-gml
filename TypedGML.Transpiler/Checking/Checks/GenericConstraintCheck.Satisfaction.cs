using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class GenericConstraintCheck
{
    private static bool SatisfiesConstraint(TgmlTypeDecl argDecl, TgmlTypeRef constraint, TranspileContext ctx)
    {
        var constraintName = constraint.Name.Full;
        var allBases = CollectAllBaseNames(argDecl, ctx, new HashSet<string>(StringComparer.Ordinal));

        return allBases.Contains(constraintName)
               || argDecl.QualifiedName == constraintName
               || argDecl.Name == constraintName;
    }

    private static HashSet<string> CollectAllBaseNames(
        TgmlTypeDecl decl,
        TranspileContext ctx,
        HashSet<string> visited)
    {
        var qualifiedName = decl.QualifiedName ?? decl.Name;
        if (!visited.Add(qualifiedName))
            return visited;

        var bases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            TgmlInterfaceDecl i => i.BaseInterfaces,
            _ => null
        };

        if (bases is null)
            return visited;

        foreach (var baseType in bases)
        {
            visited.Add(baseType.Name.Full);
            if (ctx.TypeTable.TryResolve(baseType.Name.Full, out var baseDecl) && baseDecl is not null)
                CollectAllBaseNames(baseDecl, ctx, visited);
        }

        return visited;
    }
}
