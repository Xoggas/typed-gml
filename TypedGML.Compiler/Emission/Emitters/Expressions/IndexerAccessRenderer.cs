using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class IndexerAccessRenderer
{
    public static bool TryRenderRead(IndexerAccessExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!TryResolveIndexer(expression, ctx, out var owner, out var member))
            return false;

        var target = ctx.Emitter.Render(expression.Target, ctx);
        var index = RenderIndex(expression.Index, member, ctx);
        rendered = $"{NamingConvention.IndexerGetter(owner)}({target}, {index})";
        return true;
    }

    public static bool TryRenderAssignment(AssignmentExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (expression.Target is not IndexerAccessExpressionNode indexer ||
            !TryResolveIndexer(indexer, ctx, out var owner, out var member))
            return false;

        var target = ctx.Emitter.Render(indexer.Target, ctx);
        var index = RenderIndex(indexer.Index, member, ctx);
        var value = RenderAssignmentValue(expression, owner, member, target, index, ctx);
        rendered = $"{NamingConvention.IndexerSetter(owner)}({target}, {index}, {value})";
        return true;
    }

    private static bool TryResolveIndexer(
        IndexerAccessExpressionNode expression,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member)
    {
        var targetTypeRef = ExpressionTypeLookup.Resolve(expression.Target, ctx);
        if (string.IsNullOrWhiteSpace(targetTypeRef) ||
            targetTypeRef.EndsWith("[]", StringComparison.Ordinal) ||
            !ExpressionSymbolHelper.TryResolveType(ctx, targetTypeRef, out var targetType) ||
            targetType.Kind is not (TypeKind.Class or TypeKind.Struct))
        {
            owner = null!;
            member = null!;
            return false;
        }

        var candidates = IndexerCandidates(targetType, targetTypeRef).ToList();
        var picked = EmissionOverloadResolver.Pick(
            candidates.Select(candidate => candidate.Effective).ToList(),
            [expression.Index],
            [],
            ctx);
        var selected = candidates.FirstOrDefault(candidate => ReferenceEquals(candidate.Effective, picked)) ??
            candidates.FirstOrDefault();

        owner = selected?.Owner!;
        member = selected?.Effective!;
        return selected is not null;
    }

    private static IEnumerable<IndexerCandidate> IndexerCandidates(TypeSymbol type, string receiverTypeRef)
    {
        for (var current = type; current is not null; current = current.Base)
        {
            var map = GenericTypeSubstitution.Map(current, receiverTypeRef);
            foreach (var member in current.Members.Where(member => member.Kind == MemberKind.Indexer))
                yield return new IndexerCandidate(current, GenericTypeSubstitution.Substitute(member, map));
        }
    }

    private static string RenderIndex(IAstNode index, MemberSymbol member, EmitContext ctx) =>
        ctx.RenderWithExpected(index, member.Parameters.FirstOrDefault()?.TypeRef);

    private static string RenderAssignmentValue(
        AssignmentExpressionNode expression,
        TypeSymbol owner,
        MemberSymbol member,
        string target,
        string index,
        EmitContext ctx)
    {
        var value = ctx.RenderWithExpected(expression.Value, member.ReturnType);
        if (expression.Op == "=")
            return value;

        var op = expression.Op.EndsWith("=", StringComparison.Ordinal) ? expression.Op[..^1] : expression.Op;
        var read = $"{NamingConvention.IndexerGetter(owner)}({target}, {index})";
        return $"{read} {op} {value}";
    }

    private sealed record IndexerCandidate(TypeSymbol Owner, MemberSymbol Effective);
}
