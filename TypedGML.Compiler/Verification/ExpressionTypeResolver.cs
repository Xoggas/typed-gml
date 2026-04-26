using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class ExpressionTypeResolver
{
    public static string? Resolve(IAstNode? node, VerificationContext ctx) => node switch
    {
        null => null,
        LiteralExpressionNode literal => literal.Kind switch
        {
            LiteralKind.Number => "number",
            LiteralKind.String => "string",
            LiteralKind.Bool => "bool",
            LiteralKind.Null => "null",
            _ => null
        },
        IdentifierExpressionNode identifier => ResolveIdentifier(identifier, ctx),
        AssignmentExpressionNode assignment => Resolve(assignment.Target, ctx),
        ObjectCreationExpressionNode creation => creation.TypeRef,
        CastExpressionNode cast => cast.CastKind == CastKind.Is ? "bool" : cast.TargetType,
        TypeofExpressionNode or NameofExpressionNode => "string",
        DefaultExpressionNode defaultValue => defaultValue.TypeName,
        ArrayLiteralExpressionNode array => ResolveArray(array, ctx),
        MemberAccessExpressionNode access => ResolveMemberAccess(access, ctx),
        InvocationExpressionNode invocation => ResolveInvocation(invocation, ctx),
        BinaryExpressionNode binary => OperatorResolutionHelper.ResolveBinaryResult(
            binary.Op,
            Resolve(binary.Left, ctx),
            Resolve(binary.Right, ctx),
            ctx),
        UnaryExpressionNode unary => OperatorResolutionHelper.ResolveUnaryResult(
            unary.Op,
            Resolve(unary.Operand, ctx),
            ctx),
        TernaryExpressionNode ternary => Resolve(ternary.ThenExpr, ctx) == Resolve(ternary.ElseExpr, ctx)
            ? Resolve(ternary.ThenExpr, ctx)
            : null,
        NullCoalescingExpressionNode coalescing => TypeReferenceHelper.UnwrapNullable(Resolve(coalescing.Left, ctx)),
        IndexerAccessExpressionNode indexer => ResolveIndexer(indexer, ctx),
        BaseAccessExpressionNode access => ResolveBaseMember(access.MemberName, ctx),
        BaseCallExpressionNode call => ResolveBaseMember(call.MemberName, ctx),
        _ => null
    };

    private static string? ResolveIdentifier(IdentifierExpressionNode identifier, VerificationContext ctx)
    {
        if (identifier.Name == "this")
            return ctx.CurrentType?.QualifiedName;

        if (identifier.Name == "base")
            return ctx.CurrentType?.Base?.QualifiedName;

        if (ctx.Scope.TryResolve(identifier.Name, out var scopedType))
            return scopedType;

        var member = SymbolResolver.FindMember(ctx.CurrentType, identifier.Name, out _);
        if (member is not null)
            return member.ReturnType;

        return SymbolResolver.TryResolveType(identifier.Name, ctx, out var type)
            ? type.QualifiedName
            : null;
    }

    private static string? ResolveMemberAccess(MemberAccessExpressionNode access, VerificationContext ctx)
    {
        if (!SymbolResolver.TryResolveType(Resolve(access.Target, ctx), ctx, out var targetType))
            return null;

        return SymbolResolver.FindMember(targetType, access.MemberName, out _)?.ReturnType;
    }

    private static string? ResolveInvocation(InvocationExpressionNode invocation, VerificationContext ctx)
    {
        if (invocation.Target is MemberAccessExpressionNode access)
            return ResolveMemberAccess(access, ctx);

        if (invocation.Target is IdentifierExpressionNode identifier)
        {
            var member = SymbolResolver.FindMember(ctx.CurrentType, identifier.Name, out _);
            return member?.ReturnType;
        }

        var targetType = Resolve(invocation.Target, ctx);
        return targetType is null ? null : ResolveDelegateReturnType(targetType, ctx);
    }

    private static string? ResolveIndexer(IndexerAccessExpressionNode indexer, VerificationContext ctx)
    {
        var targetType = Resolve(indexer.Target, ctx);
        if (string.IsNullOrWhiteSpace(targetType))
            return null;

        if (targetType.EndsWith("[]", StringComparison.Ordinal))
            return targetType[..^2];

        if (!SymbolResolver.TryResolveType(targetType, ctx, out var resolved))
            return null;

        return resolved.Members.FirstOrDefault(m => m.Kind == MemberKind.Indexer)?.ReturnType;
    }

    private static string? ResolveBaseMember(string memberName, VerificationContext ctx) =>
        SymbolResolver.FindMember(ctx.CurrentType?.Base, memberName, out _)?.ReturnType;

    private static string? ResolveArray(ArrayLiteralExpressionNode array, VerificationContext ctx)
    {
        if (array.Elements.Count == 0)
            return null;

        var elementType = Resolve(array.Elements[0], ctx);
        return elementType is null || array.Elements.Skip(1).Any(e => Resolve(e, ctx) != elementType)
            ? null
            : $"{elementType}[]";
    }

    private static string? ResolveDelegateReturnType(string typeRef, VerificationContext ctx)
    {
        if (!SymbolResolver.TryResolveType(typeRef, ctx, out var symbol))
            return null;

        return symbol.Members.FirstOrDefault(m => m.Kind == MemberKind.Method)?.ReturnType;
    }
}
