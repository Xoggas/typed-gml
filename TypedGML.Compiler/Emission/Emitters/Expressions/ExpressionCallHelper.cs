using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ExpressionCallHelper
{
    public static string JoinArguments(
        IAstNode target,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx) =>
        string.Join(", ", OrderArguments(target, positionalArgs, namedArgs, ctx));

    public static string JoinConstructorArguments(
        TypeSymbol? type,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx) =>
        string.Join(", ", Order(positionalArgs, namedArgs, type?.Members.Where(m => m.Kind == MemberKind.Constructor).ToList(), ctx));

    public static IReadOnlyList<string> ObjectCtorArguments(
        TypeSymbol? type,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx) =>
        Order(positionalArgs.Skip(3).ToList(), namedArgs, type?.Members.Where(m => m.Kind == MemberKind.Constructor).ToList(), ctx);

    public static string RenderTarget(IAstNode target, EmitContext ctx) => target switch
    {
        _ when StaticMemberAccessHelper.TryRenderRead(target, ctx, out var rendered) => rendered,
        MemberAccessExpressionNode access => $"{ctx.Emitter.Render(access.Target, ctx)}.{access.MemberName}",
        _ => ctx.Emitter.Render(target, ctx)
    };

    private static IReadOnlyList<string> OrderArguments(
        IAstNode target,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        EmitContext ctx)
    {
        var candidates = target switch
        {
            IdentifierExpressionNode identifier when ExpressionSymbolHelper.TryResolveCurrentMember(ctx, identifier.Name, out var _) =>
                ctx.CurrentType?.Members.Where(m => m.Kind == MemberKind.Method && m.Name == identifier.Name).ToList(),
            MemberAccessExpressionNode access when ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type) =>
                type.Members.Where(m => m.Kind == MemberKind.Method && m.Name == access.MemberName).ToList(),
            _ => null
        };

        return Order(positionalArgs, namedArgs, candidates, ctx);
    }

    private static IReadOnlyList<string> Order(
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        IReadOnlyList<MemberSymbol>? candidates,
        EmitContext ctx)
    {
        var candidate = PickCandidate(positionalArgs, namedArgs, candidates);
        if (candidate is null)
            return positionalArgs.Select(a => ctx.Emitter.Render(a, ctx)).Concat(namedArgs.Select(a => ctx.Emitter.Render(a.Value, ctx))).ToList();

        var values = new string[candidate.Parameters.Count];
        for (var i = 0; i < positionalArgs.Count && i < values.Length; i++)
            values[i] = ctx.Emitter.Render(positionalArgs[i], ctx);
        foreach (var namedArg in namedArgs)
        {
            var index = candidate.Parameters.ToList().FindIndex(p => p.Name == namedArg.Name);
            if (index >= 0)
                values[index] = ctx.Emitter.Render(namedArg.Value, ctx);
        }

        for (var i = 0; i < values.Length; i++)
            if (string.IsNullOrEmpty(values[i]) && candidate.Parameters[i].DefaultValue is IAstNode defaultValue)
                values[i] = ctx.Emitter.Render(defaultValue, ctx);

        return values.Where(v => !string.IsNullOrEmpty(v)).ToList();
    }

    private static MemberSymbol? PickCandidate(
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        IReadOnlyList<MemberSymbol>? candidates) =>
        candidates?.FirstOrDefault(candidate =>
            positionalArgs.Count <= candidate.Parameters.Count &&
            positionalArgs.Count + namedArgs.Count <= candidate.Parameters.Count &&
            namedArgs.All(arg => candidate.Parameters.Any(p => p.Name == arg.Name)));
}
