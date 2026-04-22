using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class GenerationContext
{
    public bool TryFindBaseMethod(
        TgmlClassDecl owner,
        string methodName,
        out TgmlClassDecl declaringType,
        out TgmlMethodDecl method)
    {
        return TryFindBaseMethod(owner.BaseTypes, methodName, new HashSet<string>(StringComparer.Ordinal), out declaringType, out method);
    }

    public List<TgmlClassDecl> GetGameObjectAncestorChain(TgmlClassDecl cls)
    {
        var chain = new List<TgmlClassDecl>();
        CollectGameObjectAncestors(cls.BaseTypes, new HashSet<string>(StringComparer.Ordinal), chain);
        return chain;
    }

    public List<TgmlClassDecl> GetClassAncestorChain(TgmlClassDecl cls)
    {
        var chain = new List<TgmlClassDecl>();
        CollectClassAncestors(cls.BaseTypes, new HashSet<string>(StringComparer.Ordinal), chain);
        return chain;
    }

    private void CollectGameObjectAncestors(
        IEnumerable<TgmlTypeRef> baseTypes,
        HashSet<string> visited,
        List<TgmlClassDecl> chain)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            CollectGameObjectAncestors(baseClass.BaseTypes, visited, chain);
            if (baseClass.IsGameObject)
                chain.Add(baseClass);
        }
    }

    private void CollectClassAncestors(
        IEnumerable<TgmlTypeRef> baseTypes,
        HashSet<string> visited,
        List<TgmlClassDecl> chain)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            CollectClassAncestors(baseClass.BaseTypes, visited, chain);
            chain.Add(baseClass);
        }
    }

    private bool TryFindBaseMethod(
        IEnumerable<TgmlTypeRef> baseTypes,
        string methodName,
        HashSet<string> visited,
        out TgmlClassDecl declaringType,
        out TgmlMethodDecl method)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            var found = baseClass.Methods.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            if (found is not null)
            {
                declaringType = baseClass;
                method = found;
                return true;
            }

            if (TryFindBaseMethod(baseClass.BaseTypes, methodName, visited, out declaringType, out method))
                return true;
        }

        declaringType = null!;
        method = null!;
        return false;
    }
}
