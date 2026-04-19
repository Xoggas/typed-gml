using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed record DelegateSignature(
    string TypeName,
    string ReturnType,
    IReadOnlyList<DelegateParameter> Parameters);

public sealed record DelegateParameter(string Name, string TypeName);

public sealed record DelegateRuntimeModel(
    string TypeName,
    string RuntimeTypeName,
    string CreateHelperName,
    string CombineHelperName,
    string RemoveHelperName,
    string InvokeHelperName);

public static class DelegateFacts
{
    public static bool TryResolveSignature(TypeTable typeTable, string describedType, out DelegateSignature signature)
    {
        var (baseName, typeArgs) = SplitDescribedType(describedType);
        if (!typeTable.TryResolve(baseName, typeArgs.Count, out var decl) || decl is not TgmlDelegateDecl delegateDecl)
        {
            signature = null!;
            return false;
        }

        var substitutions = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < Math.Min(delegateDecl.TypeParams.Count, typeArgs.Count); i++)
            substitutions[delegateDecl.TypeParams[i].Name] = typeArgs[i];

        signature = new DelegateSignature(
            describedType,
            Substitute(delegateDecl.ReturnType, substitutions),
            delegateDecl.Params
                .Select(p => new DelegateParameter(p.Name, Substitute(p.Type, substitutions)))
                .ToList());
        return true;
    }

    public static bool IsDelegateType(TypeTable typeTable, string describedType)
    {
        var (baseName, typeArgs) = SplitDescribedType(describedType);
        return typeTable.TryResolve(baseName, typeArgs.Count, out var decl) && decl is TgmlDelegateDecl;
    }

    public static bool TryResolveRuntimeModel(TypeTable typeTable, string describedType, out DelegateRuntimeModel model)
    {
        var (baseName, typeArgs) = SplitDescribedType(describedType);
        if (!typeTable.TryResolve(baseName, typeArgs.Count, out var decl) || decl is not TgmlDelegateDecl delegateDecl)
        {
            model = null!;
            return false;
        }

        var runtimeTypeName = GetRuntimeTypeName(delegateDecl);
        model = new DelegateRuntimeModel(
            describedType,
            runtimeTypeName,
            $"{runtimeTypeName}__create",
            $"{runtimeTypeName}__combine",
            $"{runtimeTypeName}__remove",
            $"{runtimeTypeName}__invoke");
        return true;
    }

    public static string GetRuntimeTypeName(TgmlDelegateDecl delegateDecl)
    {
        var qualifiedName = delegateDecl.QualifiedName ?? delegateDecl.Name;
        return $"{qualifiedName.Replace(".", "_")}__delegate_{delegateDecl.TypeParams.Count}";
    }

    private static string Substitute(TgmlTypeRef typeRef, IReadOnlyDictionary<string, string> substitutions)
    {
        if (typeRef.ArrayDepth == 0 &&
            typeRef.TypeArgs.Count == 0 &&
            substitutions.TryGetValue(typeRef.Name.Full, out var mapped))
        {
            return mapped;
        }

        if (typeRef.TypeArgs.Count == 0)
            return typeRef.ArrayDepth == 0 ? typeRef.Name.Full : typeRef.ToString();

        var typeArgs = string.Join(", ", typeRef.TypeArgs.Select(arg => Substitute(arg, substitutions)));
        var arraySuffix = string.Concat(Enumerable.Repeat("[]", typeRef.ArrayDepth));
        return $"{typeRef.Name.Full}<{typeArgs}>{arraySuffix}";
    }

    public static (string BaseName, List<string> TypeArgs) SplitDescribedType(string describedType)
    {
        var genericStart = describedType.IndexOf('<');
        if (genericStart < 0)
            return (describedType, []);

        var baseName = describedType[..genericStart];
        var depth = 0;
        var end = -1;
        for (var i = genericStart; i < describedType.Length; i++)
        {
            switch (describedType[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }
                    break;
            }

            if (end >= 0)
                break;
        }

        if (end < 0)
            return (baseName, []);

        var genericPayload = describedType[(genericStart + 1)..end];
        return (baseName, SplitTopLevelTypes(genericPayload));
    }

    private static List<string> SplitTopLevelTypes(string payload)
    {
        var parts = new List<string>();
        var start = 0;
        var depth = 0;

        for (var i = 0; i < payload.Length; i++)
        {
            switch (payload[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(payload[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(payload[start..].Trim());
        return parts;
    }
}
