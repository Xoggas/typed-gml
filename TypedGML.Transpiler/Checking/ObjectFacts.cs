using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static class ObjectFacts
{
    public const string SystemObjectQualifiedName = "System.Object";
    public const string GetTypeMethodName = "GetType";
    public const string ToStringMethodName = "ToString";
    public const string ResolvedStringLiteralConversionMetadata = "ResolvedStringLiteralConversion";

    private const string SynthesizedGetTypeMethodMetadata = "SynthesizedGetTypeMethod";

    public static bool IsSystemObject(TgmlTypeDecl decl)
        => string.Equals(decl.QualifiedName ?? decl.Name, SystemObjectQualifiedName, StringComparison.Ordinal);

    public static bool HasImplicitObjectBase(TgmlTypeDecl decl, TypeTable typeTable)
    {
        if (IsSystemObject(decl))
            return false;

        return decl switch
        {
            TgmlClassDecl cls => !cls.IsStatic && !HasExplicitClassBase(cls.BaseTypes, typeTable),
            TgmlStructDecl => true,
            _ => false
        };
    }

    public static bool TryResolveImplicitObject(TypeTable typeTable, TgmlTypeDecl decl, out TgmlClassDecl systemObject)
    {
        if (HasImplicitObjectBase(decl, typeTable) &&
            typeTable.TryResolve(SystemObjectQualifiedName, out var objectDecl) &&
            objectDecl is TgmlClassDecl objectClass)
        {
            systemObject = objectClass;
            return true;
        }

        systemObject = null!;
        return false;
    }

    public static TgmlMethodDecl GetOrCreateSynthesizedGetTypeMethod(TgmlTypeDecl decl)
    {
        if (decl.Metadata.TryGetValue(SynthesizedGetTypeMethodMetadata, out var cached) && cached is TgmlMethodDecl method)
            return method;

        method = new TgmlMethodDecl
        {
            Name = GetTypeMethodName,
            Access = AccessModifier.Public,
            ReturnType = new TgmlTypeRef { Name = new TgmlQualifiedName { Parts = ["number"] } },
            Modifiers = new MethodModifiers(AccessModifier.Public, false, VirtualModifier.None)
        };
        decl.Metadata[SynthesizedGetTypeMethodMetadata] = method;
        return method;
    }

    private static bool HasExplicitClassBase(IEnumerable<TgmlTypeRef> baseTypes, TypeTable typeTable)
    {
        foreach (var baseRef in baseTypes)
        {
            if (typeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is TgmlClassDecl)
                return true;
        }

        return false;
    }
}
