using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private HashSet<string> CollectReachableTypeNames(TgmlTypeDecl decl, HashSet<string> visited)
    {
        var qualifiedName = decl.QualifiedName ?? decl.Name;
        if (!visited.Add(qualifiedName))
            return visited;

        visited.Add(decl.Name);

        var bases = decl switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            TgmlInterfaceDecl iface => iface.BaseInterfaces,
            _ => null
        };

        if (bases is null)
            return visited;

        if (ObjectFacts.TryResolveImplicitObject(_ctx.TypeTable, decl, out var systemObject))
        {
            visited.Add("Object");
            visited.Add(ObjectFacts.SystemObjectQualifiedName);
            CollectReachableTypeNames(systemObject, visited);
            return visited;
        }

        foreach (var baseRef in bases)
        {
            visited.Add(baseRef.Name.Full);
            if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                CollectReachableTypeNames(baseDecl, visited);
        }

        return visited;
    }

    private bool TryResolveTypeDecl(string typeName, out TgmlTypeDecl? decl)
    {
        var (baseTypeName, typeArgs) = DelegateFacts.SplitDescribedType(typeName);
        var typeArgCount = typeArgs.Count;

        while (baseTypeName.EndsWith("[]", StringComparison.Ordinal))
            baseTypeName = baseTypeName[..^2];

        return _ctx.TypeTable.TryResolve(baseTypeName, typeArgCount, out decl);
    }

    private static void SetResolvedOperator(TgmlExpression expr, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        expr.Metadata[ResolvedOperatorOwnerMetadata] = owner;
        expr.Metadata[ResolvedOperatorMethodMetadata] = method;
    }

    private static void ClearResolvedOperator(TgmlExpression expr)
    {
        expr.Metadata.Remove(ResolvedOperatorOwnerMetadata);
        expr.Metadata.Remove(ResolvedOperatorMethodMetadata);
    }

    private static void SetResolvedConversion(TgmlExpression expr, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        expr.Metadata[ResolvedConversionOwnerMetadata] = owner;
        expr.Metadata[ResolvedConversionMethodMetadata] = method;
    }

    private static void ClearResolvedConversion(TgmlExpression expr)
    {
        expr.Metadata.Remove(ResolvedConversionOwnerMetadata);
        expr.Metadata.Remove(ResolvedConversionMethodMetadata);
    }

    // -- Utilities -------------------------------------------------------------

    /// <summary>Checks that <paramref name="valueExpr" />'s type is assignable to <paramref name="targetType" />.</summary>
}
