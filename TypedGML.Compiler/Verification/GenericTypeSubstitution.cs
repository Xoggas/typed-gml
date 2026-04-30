using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class GenericTypeSubstitution
{
    public static IReadOnlyDictionary<string, string> Map(TypeSymbol type, IReadOnlyList<string> typeArgs) =>
        type.GenericParameters.Zip(typeArgs)
            .ToDictionary(pair => pair.First.Name, pair => pair.Second, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, string> Map(TypeSymbol type, string? typeRef)
    {
        var typeArgs = TypeReferenceHelper.TypeArguments(typeRef);
        return type.GenericParameters.Count == typeArgs.Count
            ? Map(type, typeArgs)
            : new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static string Substitute(string typeRef, IReadOnlyDictionary<string, string> map)
    {
        if (map.Count == 0 || string.IsNullOrWhiteSpace(typeRef))
            return typeRef;

        var (core, suffix) = SplitSuffix(typeRef);
        if (map.TryGetValue(core, out var replacement))
            return replacement + suffix;

        var genericStart = core.IndexOf('<');
        if (genericStart < 0)
            return typeRef;

        var args = TypeReferenceHelper.TypeArguments(core)
            .Select(arg => Substitute(arg, map));
        return $"{core[..genericStart]}<{string.Join(", ", args)}>{suffix}";
    }

    public static MemberSymbol Substitute(MemberSymbol member, IReadOnlyDictionary<string, string> map) =>
        map.Count == 0
            ? member
            : new MemberSymbol
            {
                Name = member.Name,
                Kind = member.Kind,
                ReturnType = Substitute(member.ReturnType, map),
                Parameters = member.Parameters
                    .Select(parameter => parameter with { TypeRef = Substitute(parameter.TypeRef, map) })
                    .ToList(),
                GenericParameters = member.GenericParameters,
                Modifiers = member.Modifiers,
                ConstValue = member.ConstValue,
                ConstructorChainTarget = member.ConstructorChainTarget,
                ConstructorChainArgs = member.ConstructorChainArgs,
                NativeEventName = member.NativeEventName,
                NativePropertyName = member.NativePropertyName,
                AssetName = member.AssetName,
                NativeCallName = member.NativeCallName
            };

    public static TypeSymbol ApplySubstitution(TypeSymbol type, IReadOnlyDictionary<string, string> map)
    {
        if (map.Count == 0)
            return type;

        var substituted = new TypeSymbol
        {
            QualifiedName = type.QualifiedName,
            Kind = type.Kind,
            Base = type.Base,
            IsAbstract = type.IsAbstract,
            IsSealed = type.IsSealed,
            ObjectAssetName = type.ObjectAssetName,
            BclTypeName = type.BclTypeName
        };
        substituted.GenericParameters.AddRange(type.GenericParameters);
        substituted.Interfaces.AddRange(type.Interfaces);
        substituted.Members.AddRange(type.Members.Select(member => Substitute(member, map)));
        return substituted;
    }

    private static (string Core, string Suffix) SplitSuffix(string typeRef)
    {
        var core = typeRef;
        var suffix = string.Empty;
        while (core.EndsWith("?", StringComparison.Ordinal) || core.EndsWith("[]", StringComparison.Ordinal))
        {
            if (core.EndsWith("?", StringComparison.Ordinal))
            {
                suffix = "?" + suffix;
                core = core[..^1];
            }
            else
            {
                suffix = "[]" + suffix;
                core = core[..^2];
            }
        }

        return (core, suffix);
    }
}
