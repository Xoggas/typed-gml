using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class OperatorResolutionHelper
{
    public static string? ResolveBinaryResult(string op, string? leftType, string? rightType, VerificationContext ctx)
    {
        if (op is "and" or "or" or "==" or "!=")
            return "bool";

        if (IsStringEnumConcat(op, leftType, rightType, ctx))
            return "string";

        return FindBinary(op, leftType, rightType, ctx)?.ReturnType;
    }

    public static string? ResolveUnaryResult(string op, string? operandType, VerificationContext ctx) =>
        op == "not" ? "bool" : FindUnary(op, operandType, ctx)?.ReturnType;

    public static MemberSymbol? FindBinary(string op, string? leftType, string? rightType, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(leftType) || string.IsNullOrWhiteSpace(rightType))
            return null;

        if (TryResolveOperatorOwner(leftType, ctx, out var leftOwner))
        {
            var match = Match(leftOwner, op, [leftType!, rightType!]);
            if (match is not null)
                return match;
        }

        if (!TryResolveOperatorOwner(rightType, ctx, out var rightOwner))
            return null;

        return Match(rightOwner, op, [leftType!, rightType!]);
    }

    public static MemberSymbol? FindUnary(string op, string? operandType, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(operandType) || !TryResolveOperatorOwner(operandType, ctx, out var owner))
            return null;

        return Match(owner, op, [operandType!]);
    }

    public static bool IsStringEnumConcat(string op, string? leftType, string? rightType, VerificationContext ctx) =>
        op == "+" &&
        ((leftType == "string" && IsEnum(rightType, ctx)) ||
            (rightType == "string" && IsEnum(leftType, ctx)));

    private static MemberSymbol? Match(TypeSymbol owner, string op, IReadOnlyList<string> parameterTypes) =>
        owner.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Operator &&
            member.Name == op &&
            member.Parameters.Count == parameterTypes.Count &&
            member.Parameters.Select(parameter => parameter.TypeRef).SequenceEqual(parameterTypes, StringComparer.Ordinal));

    private static bool TryResolveOperatorOwner(string typeRef, VerificationContext ctx, out TypeSymbol type)
    {
        if (!SymbolResolver.TryResolveType(typeRef, ctx, out type))
            return false;

        if (type.BclTypeName is null)
            return true;

        return TryResolveBclType(type.BclTypeName, ctx, out type);
    }

    private static bool TryResolveBclType(string bclTypeName, VerificationContext ctx, out TypeSymbol type)
    {
        if (!bclTypeName.Contains('.', StringComparison.Ordinal) &&
            ctx.Symbols.TryResolve($"TypedGML.Core.{bclTypeName}", null, [], out type))
            return true;

        return ctx.Symbols.TryResolve(bclTypeName, null, [], out type);
    }

    private static bool IsEnum(string? typeRef, VerificationContext ctx) =>
        SymbolResolver.TryResolveType(typeRef, ctx, out var type) && type.Kind == TypeKind.Enum;
}
