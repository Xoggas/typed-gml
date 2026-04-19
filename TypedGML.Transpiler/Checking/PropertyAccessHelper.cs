using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Shared helpers for resolving properties across inheritance hierarchies and
///     evaluating accessor visibility.
/// </summary>
public static class PropertyAccessHelper
{
    public sealed record ResolvedProperty(TgmlTypeDecl DeclaringType, TgmlPropertyDecl Property);

    public static ResolvedProperty? FindPropertyInHierarchy(TypeTable typeTable, TgmlTypeDecl? type, string name)
    {
        return FindPropertyInHierarchy(typeTable, type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    public static ResolvedProperty? FindIndexerInHierarchy(TypeTable typeTable, TgmlTypeDecl? type)
    {
        return FindIndexerInHierarchy(typeTable, type, new HashSet<string>(StringComparer.Ordinal));
    }

    public static AccessModifier EffectiveGetterAccess(TgmlPropertyDecl property)
        => property.Getter?.AccessMod ?? property.Access;

    public static AccessModifier EffectiveSetterAccess(TgmlPropertyDecl property)
        => property.Setter?.AccessMod ?? property.Access;

    public static bool CanAccess(TypeTable typeTable, TgmlTypeDecl? accessingType, TgmlTypeDecl declaringType,
        AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => true,
            AccessModifier.Private => SameType(accessingType, declaringType),
            AccessModifier.Protected => SameType(accessingType, declaringType) ||
                                        IsSameOrDerivedFrom(typeTable, accessingType, declaringType),
            _ => false
        };
    }

    public static bool IsSameOrDerivedFrom(TypeTable typeTable, TgmlTypeDecl? type, TgmlTypeDecl ancestor)
    {
        if (type is null) return false;
        if (SameType(type, ancestor)) return true;

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };

        if (baseRefs is null) return false;

        foreach (var baseRef in baseRefs)
        {
            if (!typeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            if (SameType(baseDecl, ancestor) || IsSameOrDerivedFrom(typeTable, baseDecl, ancestor))
                return true;
        }

        return false;
    }

    private static ResolvedProperty? FindPropertyInHierarchy(
        TypeTable typeTable,
        TgmlTypeDecl? type,
        string name,
        HashSet<string> visited)
    {
        if (type is null) return null;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return null;

        var own = type switch
        {
            TgmlClassDecl cls => cls.Properties.FirstOrDefault(p => p.Name == name),
            TgmlStructDecl str => str.Properties.FirstOrDefault(p => p.Name == name),
            _ => null
        };
        if (own is not null) return new ResolvedProperty(type, own);

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (baseRefs is null) return null;

        foreach (var baseRef in baseRefs)
        {
            if (!typeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindPropertyInHierarchy(typeTable, baseDecl, name, visited);
            if (found is not null) return found;
        }

        return null;
    }

    private static ResolvedProperty? FindIndexerInHierarchy(
        TypeTable typeTable,
        TgmlTypeDecl? type,
        HashSet<string> visited)
    {
        if (type is null) return null;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return null;

        var own = type switch
        {
            TgmlClassDecl cls => cls.Properties.FirstOrDefault(p => p.IsIndexer),
            TgmlStructDecl str => str.Properties.FirstOrDefault(p => p.IsIndexer),
            _ => null
        };
        if (own is not null) return new ResolvedProperty(type, own);

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (baseRefs is null) return null;

        foreach (var baseRef in baseRefs)
        {
            if (!typeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindIndexerInHierarchy(typeTable, baseDecl, visited);
            if (found is not null) return found;
        }

        return null;
    }

    private static bool SameType(TgmlTypeDecl? a, TgmlTypeDecl? b)
    {
        if (a is null || b is null) return false;

        var aKey = a.QualifiedName ?? a.Name;
        var bKey = b.QualifiedName ?? b.Name;
        return StringComparer.Ordinal.Equals(aKey, bKey);
    }
}
