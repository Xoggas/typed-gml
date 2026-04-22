using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private TgmlFieldDecl? FindFieldInHierarchy(TgmlTypeDecl? type, string name)
    {
        return FindFieldInHierarchy(type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    /// <summary>Returns the field and the type in which it was declared.</summary>
    private (TgmlFieldDecl? Field, TgmlTypeDecl? DeclaringType) FindFieldWithDecl(TgmlTypeDecl? type, string name)
    {
        return FindFieldWithDecl(type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    private (TgmlFieldDecl? Field, TgmlTypeDecl? DeclaringType) FindFieldWithDecl(
        TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return (null, null);

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return (null, null);

        var own = type switch
        {
            TgmlClassDecl cls => cls.Fields.FirstOrDefault(f => f.Name == name),
            TgmlStructDecl str => str.Fields.FirstOrDefault(f => f.Name == name),
            _ => null
        };
        if (own is not null) return (own, type);

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (baseRefs is null) return (null, null);

        foreach (var baseRef in baseRefs)
        {
            if (!_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindFieldWithDecl(baseDecl, name, visited);
            if (found.Field is not null) return found;
        }

        return (null, null);
    }

    private TgmlClassDecl? ResolveBaseClass(TgmlClassDecl ownerClass)
    {
        foreach (var baseRef in ownerClass.BaseTypes)
        {
            if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is TgmlClassDecl baseClass)
                return baseClass;
        }

        return null;
    }

    private List<TgmlMethodDecl> FindMethodsInHierarchy(TgmlTypeDecl? type, string name)
    {
        return FindMethodsInHierarchy(type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    private List<TgmlMethodDecl> FindMethodsInHierarchy(TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return [];

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return [];

        var methods = type switch
        {
            TgmlClassDecl cls => cls.Methods.Where(m => m.Name == name).ToList(),
            TgmlStructDecl str => str.Methods.Where(m => m.Name == name).ToList(),
            TgmlInterfaceDecl iface => iface.Methods
                .Where(m => m.Name == name)
                .Select(m => new TgmlMethodDecl
                {
                    Name = m.Name,
                    Access = AccessModifier.Public,
                    Modifiers = new MethodModifiers(AccessModifier.Public, false, VirtualModifier.None),
                    ReturnType = m.ReturnType,
                    Params = m.Params,
                    TypeParams = m.TypeParams,
                    Decorators = m.Decorators,
                    Body = m.Body
                }).ToList(),
            _ => []
        };

        if (methods.Count > 0)
            return methods;

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };

        if (baseRefs is null) return [];

        foreach (var baseRef in baseRefs)
        {
            if (!_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindMethodsInHierarchy(baseDecl, name, visited);
            if (found.Count > 0)
                return found;
        }

        // Implicit fallback to System.Object for types with no explicit base
        if (type is TgmlClassDecl { BaseTypes.Count: 0 } or TgmlStructDecl { BaseTypes.Count: 0 })
        {
            var typeFqn = type.QualifiedName ?? type.Name;
            if (typeFqn != "System.Object" &&
                _ctx.TypeTable.TryResolve("System.Object", out var objDecl) && objDecl is not null)
            {
                var objFound = FindMethodsInHierarchy(objDecl, name, visited);
                if (objFound.Count > 0) return objFound;
            }
        }

        return [];
    }

    /// <summary>
    ///     Maps a primitive TypedGML type name to its BCL wrapper class for member lookup.
    ///     e.g. "string" → "System.String", "number" → "System.Number"
    /// </summary>
    private static string? MapPrimitiveType(string? typeName) => typeName switch
    {
        "string" => "System.String",
        "number" or "int" or "real" => "System.Number",
        "bool" => "System.Bool",
        _ when typeName is not null && typeName.EndsWith("[]") => "System.Array",
        _ => typeName
    };

    private TgmlFieldDecl? FindFieldInHierarchy(TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return null;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return null;

        var own = type switch
        {
            TgmlClassDecl cls => cls.Fields.FirstOrDefault(f => f.Name == name),
            TgmlStructDecl str => str.Fields.FirstOrDefault(f => f.Name == name),
            _ => null
        };
        if (own is not null) return own;

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (baseRefs is null) return null;

        foreach (var baseRef in baseRefs)
        {
            if (!_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindFieldInHierarchy(baseDecl, name, visited);
            if (found is not null) return found;
        }

        return null;
    }
}
