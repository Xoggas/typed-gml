using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ExpressionSymbolHelper
{
    public static bool TryResolveType(EmitContext ctx, string? typeRef, out TypeSymbol symbol) =>
        ctx.Symbols.TryResolve(TypeHelper.UnwrapNullable(typeRef ?? string.Empty), CurrentNamespace(ctx), ctx.UsingPrefixes, out symbol);

    public static bool IsDelegateTarget(IAstNode target, EmitContext ctx) =>
        TryResolveDelegateMember(target, ctx, out _) ||
        TryResolveTargetType(target, ctx, out var type) && type.Kind == TypeKind.Delegate;

    public static bool TryResolveDelegateMember(IAstNode target, EmitContext ctx, out MemberSymbol member)
    {
        member = null!;
        if (target is IdentifierExpressionNode identifier)
            return TryResolveCurrentMember(ctx, identifier.Name, out member) && IsDelegateLike(member, ctx);

        if (target is not MemberAccessExpressionNode access || !TryResolveTargetType(access.Target, ctx, out var type))
            return false;

        member = type.Members.FirstOrDefault(m => m.Name == access.MemberName)!;
        return member is not null && IsDelegateLike(member, ctx);
    }

    public static bool TryResolveTargetType(IAstNode target, EmitContext ctx, out TypeSymbol type)
    {
        if (QualifiedTypeAccessResolver.TryResolveType(target, ctx, out type))
            return true;

        if (target is ThisExpressionNode && ctx.CurrentType is not null)
        {
            type = ctx.CurrentType;
            return true;
        }

        if (target is IdentifierExpressionNode identifier)
        {
            if (identifier.Name == "this" && ctx.CurrentType is not null) { type = ctx.CurrentType; return true; }
            if (identifier.Name == "base" && ctx.CurrentType?.Base is not null) { type = ctx.CurrentType.Base; return true; }
            var resolvedTypeRef = ResolveIdentifierTypeRef(identifier, ctx);
            if (!string.IsNullOrWhiteSpace(resolvedTypeRef) && TryResolveType(ctx, resolvedTypeRef, out type))
                return true;
            if (TryResolveCurrentMember(ctx, identifier.Name, out var member))
                return TryResolveType(ctx, member.ReturnType, out type);
            if (TryResolveType(ctx, identifier.Name, out type))
                return true;
            if (TryResolveBclAlias(identifier.Name, ctx, out type))
                return true;
            return false;
        }

        if (target is MemberAccessExpressionNode access)
        {
            var memberType = ExpressionTypeLookup.Resolve(access, ctx);
            if (!string.IsNullOrWhiteSpace(memberType))
                return TryResolveType(ctx, memberType, out type);

            type = null!;
            return false;
        }

        if (target is ObjectCreationExpressionNode creation)
            return ctx.Symbols.TryResolve(creation.TypeRef, creation.TypeArgs.Count, CurrentNamespace(ctx), ctx.UsingPrefixes, out type);

        if (target is CastExpressionNode cast)
            return TryResolveType(ctx, cast.TargetType, out type);

        if (target is DefaultExpressionNode defaultValue)
            return TryResolveType(ctx, defaultValue.TypeName, out type);

        if (target is InvocationExpressionNode or NullConditionalExpressionNode or NullCoalescingExpressionNode or TernaryExpressionNode)
        {
            var resolvedType = ExpressionTypeLookup.Resolve(target, ctx);
            if (!string.IsNullOrWhiteSpace(resolvedType) && TryResolveType(ctx, resolvedType, out type))
                return true;
        }

        type = null!;
        return false;
    }

    public static bool TryResolveCurrentMember(EmitContext ctx, string name, out MemberSymbol member)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            member = current.Members.FirstOrDefault(m => m.Name == name)!;
            if (member is not null)
                return true;
        }

        member = null!;
        return false;
    }

    private static bool IsDelegateLike(MemberSymbol member, EmitContext ctx) =>
        member.Kind == MemberKind.Event ||
        TryResolveType(ctx, member.ReturnType, out var type) && type.Kind == TypeKind.Delegate;

    private static string? ResolveIdentifierTypeRef(IdentifierExpressionNode identifier, EmitContext ctx)
    {
        var hasScoped = ctx.Scope.TryResolve(identifier.Name, out var scopedTypeRef);
        var hasNarrowed = ctx.Narrowing.TryResolve(identifier.Name, out var narrowedTypeRef);

        return hasScoped && hasNarrowed
            ? TypeSpecificityHelper.MostSpecific(scopedTypeRef, narrowedTypeRef, ctx)
            : hasNarrowed ? narrowedTypeRef : hasScoped ? scopedTypeRef : null;
    }

    private static string CurrentNamespace(EmitContext ctx)
    {
        if (!string.IsNullOrEmpty(ctx.CurrentNamespacePrefix))
            return ctx.CurrentNamespacePrefix;
        if (ctx.CurrentType is null || !ctx.CurrentType.QualifiedName.Contains('.', StringComparison.Ordinal))
            return string.Empty;
        return ctx.CurrentType.QualifiedName[..ctx.CurrentType.QualifiedName.LastIndexOf('.')];
    }

    private static bool TryResolveBclAlias(string name, EmitContext ctx, out TypeSymbol type)
    {
        foreach (var alias in Aliases(name))
            if (TryResolveType(ctx, alias, out type))
                return true;

        type = null!;
        return false;
    }

    private static IReadOnlyList<string> Aliases(string name) => name switch
    {
        "Number" => ["Math", "number"],
        "String" => ["string"],
        "Bool" => ["bool"],
        _ => []
    };
}
