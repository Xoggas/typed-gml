using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static class EnumFacts
{
    public const string EnumMemberMacroMetadata = "EnumMemberMacro";
    public const string EnumMemberQualifiedValueMetadata = "EnumMemberQualifiedValue";

    public static bool TryResolveMember(
        TypeTable typeTable,
        TgmlExpression expr,
        out TgmlEnumDecl enumDecl,
        out TgmlEnumMember member)
    {
        if (expr is TgmlFieldAccessExpr fieldAccess)
            return TryResolveMember(typeTable, fieldAccess, out enumDecl, out member);

        enumDecl = null!;
        member = null!;
        return false;
    }

    public static bool TryResolveMember(
        TypeTable typeTable,
        TgmlFieldAccessExpr access,
        out TgmlEnumDecl enumDecl,
        out TgmlEnumMember member)
    {
        if (TryGetQualifiedTargetName(access.Target, out var targetName) &&
            typeTable.TryResolve(targetName, out var resolvedDecl) &&
            resolvedDecl is TgmlEnumDecl resolvedEnum)
        {
            var resolvedMember = resolvedEnum.Members.FirstOrDefault(m => string.Equals(m.Name, access.FieldName, StringComparison.Ordinal));
            if (resolvedMember is not null)
            {
                enumDecl = resolvedEnum;
                member = resolvedMember;
                return true;
            }
        }

        enumDecl = null!;
        member = null!;
        return false;
    }

    public static void AnnotateMemberAccess(TgmlFieldAccessExpr access, TgmlEnumDecl enumDecl, TgmlEnumMember member)
    {
        access.Metadata[EnumMemberMacroMetadata] = GetMacroName(enumDecl, member);
        access.Metadata[EnumMemberQualifiedValueMetadata] = GetQualifiedMemberName(enumDecl, member);
    }

    public static string GetMacroName(TgmlEnumDecl enumDecl, TgmlEnumMember member)
    {
        var prefix = enumDecl.QualifiedName?.Replace(".", "_") ?? enumDecl.Name;
        return $"{prefix}_{member.Name}";
    }

    public static string GetQualifiedMemberName(TgmlEnumDecl enumDecl, TgmlEnumMember member)
    {
        var enumName = enumDecl.QualifiedName ?? enumDecl.Name;
        return $"{enumName}.{member.Name}";
    }

    private static bool TryGetQualifiedTargetName(TgmlExpression expr, out string name)
    {
        switch (expr)
        {
            case TgmlIdExpr id:
                name = id.Name;
                return true;
            case TgmlFieldAccessExpr fieldAccess when TryGetQualifiedTargetName(fieldAccess.Target, out var prefix):
                name = $"{prefix}.{fieldAccess.FieldName}";
                return true;
            default:
                name = string.Empty;
                return false;
        }
    }
}
