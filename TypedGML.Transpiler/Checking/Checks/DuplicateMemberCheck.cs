using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 1: Reports an error when a type contains two members with the same name
///     (fields and properties share one namespace; methods allow overloading by signature).
///     Also errors on overloads that are ambiguous at GML runtime (e.g., int vs real).
/// </summary>
public sealed class DuplicateMemberCheck : IAtomicCheck
{
    public string Name => "DuplicateMemberCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckType(context, file, type);
    }

    private static void CheckType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                CheckMembersWithOverloads(ctx, file, cls.Name,
                    cls.Fields.Select(f => f.Name),
                    cls.Properties.Select(p => p.Name),
                    cls.Methods,
                    cls.Constructors);
                foreach (var nested in cls.NestedTypes) CheckType(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                CheckMembersWithOverloads(ctx, file, str.Name,
                    str.Fields.Select(f => f.Name),
                    str.Properties.Select(p => p.Name),
                    str.Methods,
                    str.Constructors);
                foreach (var nested in str.NestedTypes) CheckType(ctx, file, nested);
                break;

            case TgmlInterfaceDecl iface:
                CheckMethodOverloads(ctx, file, iface.Name, iface.Methods
                    .Select(m => (m.Name, m.Params)));
                break;
        }
    }

    private static void CheckMembersWithOverloads(
        TranspileContext ctx, TgmlFile file, string typeName,
        IEnumerable<string> fieldNames,
        IEnumerable<string> propNames,
        IEnumerable<TgmlMethodDecl> methods,
        IEnumerable<TgmlConstructorDecl> constructors)
    {
        // Fields and properties must have unique names
        var fieldPropSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in fieldNames.Concat(propNames))
        {
            if (!fieldPropSeen.Add(name))
                ctx.AddError($"Type '{typeName}' already defines a member named '{name}'.", file.FileName);
        }

        // Methods: same name allowed if different parameter signature
        CheckMethodOverloads(ctx, file, typeName,
            methods.Select(m => (m.Name, m.Params)));

        // Methods must not share name with fields/properties
        foreach (var m in methods)
        {
            if (fieldPropSeen.Contains(m.Name))
                ctx.AddError(
                    $"Type '{typeName}' has both a field/property and a method named '{m.Name}'.",
                    file.FileName);
        }

        // Constructors: overloads allowed, but no duplicate signatures
        CheckConstructorOverloads(ctx, file, typeName, constructors.ToList());
    }

    private static void CheckMethodOverloads(
        TranspileContext ctx, TgmlFile file, string typeName,
        IEnumerable<(string Name, List<TgmlParam> Params)> methods)
    {
        // Key: (name, comma-joined param types)
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // Group by name to detect runtime ambiguity
        var byName = new Dictionary<string, List<List<TgmlParam>>>(StringComparer.Ordinal);

        foreach (var (name, parms) in methods)
        {
            var sig = name + "(" + string.Join(", ", parms.Select(p => p.Type.Name.Full)) + ")";
            if (!seen.Add(sig))
                ctx.AddError(
                    $"Type '{typeName}' already defines a method '{name}' with the same parameter types.",
                    file.FileName);

            if (!byName.TryGetValue(name, out var list))
                byName[name] = list = new List<List<TgmlParam>>();
            list.Add(parms);
        }

        // Check runtime ambiguity: overloads of same count distinguishable only by int vs real
        foreach (var (name, overloads) in byName)
        {
            if (overloads.Count < 2) continue;
            var byCount = overloads.GroupBy(p => p.Count);
            foreach (var group in byCount)
            {
                var same = group.ToList();
                if (same.Count < 2) continue;
                // Check if all pairs are ambiguous (int/real only differences)
                for (var i = 0; i < same.Count - 1; i++)
                for (var j = i + 1; j < same.Count; j++)
                    if (AreRuntimeAmbiguous(same[i], same[j]))
                        ctx.AddError(
                            $"Method overloads '{typeName}.{name}' cannot be distinguished at GML runtime " +
                            $"because 'int' and 'real' are both numeric. Consider using different parameter counts or types.",
                            file.FileName);
            }
        }
    }

    private static void CheckConstructorOverloads(
        TranspileContext ctx, TgmlFile file, string typeName,
        List<TgmlConstructorDecl> constructors)
    {
        if (constructors.Count < 2) return;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ctor in constructors)
        {
            var sig = string.Join(", ", ctor.Params.Select(p => p.Type.Name.Full));
            if (!seen.Add(sig))
                ctx.AddError(
                    $"Type '{typeName}' already defines a constructor with parameter types ({sig}).",
                    file.FileName);
        }

        // Check runtime ambiguity between constructors
        var byCount = constructors.GroupBy(c => c.Params.Count);
        foreach (var group in byCount)
        {
            var same = group.ToList();
            if (same.Count < 2) continue;
            for (var i = 0; i < same.Count - 1; i++)
            for (var j = i + 1; j < same.Count; j++)
                if (AreRuntimeAmbiguous(same[i].Params, same[j].Params))
                    ctx.AddError(
                        $"Constructor overloads for '{typeName}' cannot be distinguished at GML runtime " +
                        $"because 'int' and 'real' are both numeric.",
                        file.FileName);
        }
    }

    /// <summary>
    ///     Returns true when two parameter lists of equal length differ only in positions
    ///     where one is 'int' and the other is 'real' (indistinguishable in GML).
    /// </summary>
    private static bool AreRuntimeAmbiguous(
        IReadOnlyList<TgmlParam> a, IReadOnlyList<TgmlParam> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            var ta = a[i].Type.Name.Full;
            var tb = b[i].Type.Name.Full;
            if (ta == tb) continue;
            // If neither is an int/real pair, overloads CAN be distinguished
            if (!IsIntOrReal(ta) || !IsIntOrReal(tb)) return false;
        }
        return true; // all differences are int/real
    }

    private static bool IsIntOrReal(string t) => t is "int" or "real";
}
