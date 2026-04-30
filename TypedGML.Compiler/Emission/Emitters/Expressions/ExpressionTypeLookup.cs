using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ExpressionTypeLookup
{
    public static string? Resolve(IAstNode? node, EmitContext ctx) => node switch
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
        ObjectCreationExpressionNode creation => creation.TypeRef,
        CastExpressionNode cast => cast.CastKind == CastKind.Is ? "bool" : cast.TargetType,
        MemberAccessExpressionNode access => ResolveMember(access, ctx)?.ReturnType,
        InvocationExpressionNode invocation => ResolveInvocation(invocation, ctx),
        BinaryExpressionNode binary => ResolveBinary(binary, ctx),
        UnaryExpressionNode unary => unary.Op == "not" ? "bool" : Resolve(unary.Operand, ctx),
        TernaryExpressionNode ternary => Resolve(ternary.ThenExpr, ctx) == Resolve(ternary.ElseExpr, ctx)
            ? Resolve(ternary.ThenExpr, ctx)
            : null,
        NullCoalescingExpressionNode coalescing => Resolve(coalescing.Left, ctx) ?? Resolve(coalescing.Right, ctx),
        NullConditionalExpressionNode conditional => Nullable(ResolveNullConditionalAccess(conditional, ctx)?.ReturnType),
        DefaultExpressionNode defaultValue => defaultValue.TypeName,
        TypeofExpressionNode or NameofExpressionNode => "string",
        _ => null
    };

    private static string? ResolveIdentifier(IdentifierExpressionNode identifier, EmitContext ctx)
    {
        if (identifier.Name == "this")
            return ctx.CurrentType?.QualifiedName;

        if (ctx.Scope.TryResolve(identifier.Name, out var typeRef))
            return typeRef;

        if (ctx.CurrentType is not null && TryResolveCurrentMember(identifier.Name, ctx, out _, out var member))
            return member.ReturnType;

        return ExpressionSymbolHelper.TryResolveType(ctx, identifier.Name, out var type)
            ? type.QualifiedName
            : null;
    }

    private static string? ResolveInvocation(InvocationExpressionNode invocation, EmitContext ctx)
    {
        if (invocation.Target is NullConditionalExpressionNode conditional)
            return Nullable(ResolveNullConditionalInvocation(conditional, invocation, ctx)?.ReturnType);

        if (invocation.Target is IdentifierExpressionNode identifier &&
            TryResolveCurrentMember(identifier.Name, ctx, out _, out var member))
            return member.ReturnType;

        return ResolveMember(invocation.Target, ctx)?.ReturnType;
    }

    private static string? ResolveBinary(BinaryExpressionNode binary, EmitContext ctx)
    {
        if (binary.Op == "+")
        {
            var left = Resolve(binary.Left, ctx);
            var right = Resolve(binary.Right, ctx);
            return left == "string" || right == "string" ? "string" : left ?? right;
        }

        return binary.Op switch
        {
            "==" or "!=" or "<" or ">" or "<=" or ">=" or "and" or "or" => "bool",
            _ => Resolve(binary.Left, ctx) ?? Resolve(binary.Right, ctx)
        };
    }

    private static MemberSymbol? ResolveMember(IAstNode target, EmitContext ctx)
    {
        if (target is MemberAccessExpressionNode qualifiedAccess &&
            QualifiedTypeAccessResolver.TryResolveMember(qualifiedAccess, ctx, out var qualifiedOwner, out var qualifiedMember))
        {
            qualifiedOwner = PrimitiveBclTypeResolver.ResolveMemberOwner(qualifiedOwner, ctx.Symbols);
            return qualifiedOwner.Members.FirstOrDefault(m => m.Name == qualifiedMember);
        }

        if (target is MemberAccessExpressionNode access &&
            ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var owner))
            return owner.Members.FirstOrDefault(m => m.Name == access.MemberName);

        return null;
    }

    private static MemberSymbol? ResolveNullConditionalAccess(NullConditionalExpressionNode conditional, EmitContext ctx) =>
        ExpressionSymbolHelper.TryResolveTargetType(conditional.Target, ctx, out var owner)
            ? owner.Members.FirstOrDefault(m => m.Name == conditional.MemberName)
            : null;

    private static MemberSymbol? ResolveNullConditionalInvocation(
        NullConditionalExpressionNode conditional,
        InvocationExpressionNode invocation,
        EmitContext ctx) =>
        ExpressionSymbolHelper.TryResolveTargetType(conditional.Target, ctx, out var owner)
            ? EmissionOverloadResolver.Pick(
                owner.Members.Where(m => m.Kind == MemberKind.Method && m.Name == conditional.MemberName).ToList(),
                invocation.PositionalArgs,
                invocation.NamedArgs,
                ctx)
            : null;

    private static string? Nullable(string? typeRef) =>
        string.IsNullOrWhiteSpace(typeRef) || typeRef == "void"
            ? typeRef
            : typeRef.EndsWith("?", StringComparison.Ordinal) ? typeRef : $"{typeRef}?";

    private static bool TryResolveCurrentMember(
        string name,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            member = current.Members.FirstOrDefault(m => m.Name == name)!;
            if (member is null)
                continue;

            owner = current;
            return true;
        }

        owner = null!;
        member = null!;
        return false;
    }
}
