using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class EmissionOverloadResolver
{
    public static MemberSymbol? Pick(
        IReadOnlyList<MemberSymbol>? candidates,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx,
        IReadOnlyDictionary<string, string>? substitutions = null)
    {
        var map = substitutions ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var matches = candidates?.Where(candidate => Matches(candidate, positionalArgs, namedArgs, ctx, map)).ToList() ?? [];
        return matches.Count == 1 ? matches[0] : null;
    }

    private static bool Matches(
        MemberSymbol candidate,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx,
        IReadOnlyDictionary<string, string> substitutions)
    {
        var suppliedCount = positionalArgs.Count + namedArgs.Count;
        if (positionalArgs.Count > candidate.Parameters.Count ||
            suppliedCount < candidate.Parameters.Count(parameter => !parameter.HasDefault) ||
            suppliedCount > candidate.Parameters.Count)
            return false;

        for (var i = 0; i < positionalArgs.Count; i++)
            if (!ArgumentMatches(Substitute(candidate.Parameters[i].TypeRef, substitutions), positionalArgs[i], ctx))
                return false;

        foreach (var namedArg in namedArgs)
        {
            var index = IndexOf(candidate, namedArg.Name);
            if (index < 0 || index < positionalArgs.Count ||
                !ArgumentMatches(Substitute(candidate.Parameters[index].TypeRef, substitutions), namedArg.Value, ctx))
                return false;
        }

        return true;
    }

    private static bool ArgumentMatches(string targetType, IAstNode value, EmitContext ctx) =>
        value is LambdaExpressionNode lambda && EmitDelegateTypeHelper.TrySignature(targetType, ctx, out _, out var parameterTypes)
            ? LambdaParametersMatch(lambda, parameterTypes)
            : IsAssignable(targetType, ExpressionTypeLookup.Resolve(value, ctx), ctx);

    private static bool LambdaParametersMatch(LambdaExpressionNode lambda, IReadOnlyList<string> parameterTypes) =>
        lambda.Parameters.Count == parameterTypes.Count &&
        lambda.Parameters.Zip(parameterTypes).All(pair => string.IsNullOrEmpty(pair.First.TypeRef) || pair.First.TypeRef == pair.Second);

    private static bool IsAssignable(string? targetType, string? sourceType, EmitContext ctx)
    {
        if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(sourceType))
            return false;
        if (targetType == sourceType)
            return true;
        if (sourceType == "null")
            return IsNullable(targetType);
        if (IsNullable(targetType))
            return IsAssignable(UnwrapNullable(targetType), sourceType, ctx);
        if (IsNullable(sourceType))
            return false;
        if (targetType == "object" && sourceType != "void")
            return true;
        if (!ExpressionSymbolHelper.TryResolveType(ctx, targetType, out var target) ||
            !ExpressionSymbolHelper.TryResolveType(ctx, sourceType, out var source))
            return false;

        if (target == source)
            return true;

        for (var current = source.Base; current is not null; current = current.Base)
            if (current == target)
                return true;

        return source.Interfaces.Any(@interface => @interface == target) ||
               HasConversion(source, sourceType, targetType) ||
               HasConversion(target, sourceType, targetType);
    }

    private static bool IsNullable(string typeRef) =>
        typeRef.EndsWith("?", StringComparison.Ordinal);

    private static string UnwrapNullable(string typeRef) =>
        IsNullable(typeRef) ? typeRef[..^1] : typeRef;

    private static bool HasConversion(TypeSymbol type, string sourceType, string targetType) =>
        type.Members.Any(member =>
            member.Kind == MemberKind.ConversionOperator &&
            member.Modifiers.Contains("static", StringComparer.Ordinal) &&
            member.ReturnType == targetType &&
            member.Parameters.Count == 1 &&
            member.Parameters[0].TypeRef == sourceType &&
            member.Name == ConversionOperatorKind.Implicit.ToString());

    private static string Substitute(string typeRef, IReadOnlyDictionary<string, string> substitutions)
    {
        if (substitutions.Count == 0)
            return typeRef;

        var rootEnd = typeRef.IndexOfAny(['<', '?', '[']);
        var root = rootEnd < 0 ? typeRef : typeRef[..rootEnd];
        return substitutions.TryGetValue(root, out var replacement)
            ? replacement + typeRef[root.Length..]
            : typeRef;
    }

    private static int IndexOf(MemberSymbol candidate, string name)
    {
        for (var i = 0; i < candidate.Parameters.Count; i++)
            if (candidate.Parameters[i].Name == name)
                return i;

        return -1;
    }
}
