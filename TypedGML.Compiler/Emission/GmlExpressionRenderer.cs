using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Emission.Emitters.Expressions;

namespace TypedGML.Compiler.Emission;

internal static class GmlExpressionRenderer
{
    public static bool CanRender(IAstNode node) => node switch
    {
        ArrayLiteralExpressionNode or AssignmentExpressionNode or BaseAccessExpressionNode or BaseCallExpressionNode or
            BinaryExpressionNode or CastExpressionNode or DefaultExpressionNode or DictionaryLiteralExpressionNode or
            IdentifierExpressionNode or IndexerAccessExpressionNode or InvocationExpressionNode or LambdaExpressionNode or
            LiteralExpressionNode or MemberAccessExpressionNode or NameofExpressionNode or NullCoalescingExpressionNode or
            NullConditionalExpressionNode or ObjectCreationExpressionNode or TernaryExpressionNode or ThisExpressionNode or
            TypeofExpressionNode or UnaryExpressionNode => true,
        _ => false
    };

    public static string Render(IAstNode node, EmitContext ctx) => node switch
    {
        ArrayLiteralExpressionNode n => ListLiteralRenderer.TryRender(n, ctx, out var listLiteral)
            ? listLiteral
            : $"[{string.Join(", ", n.Elements.Select(e => Render(e, ctx)))}]",
        AssignmentExpressionNode n => AssignmentExpressionEmitter.Render(n, ctx),
        BaseAccessExpressionNode n => n.MemberName,
        BaseCallExpressionNode n => BaseCallInlineRenderer.Render(n, ctx),
        BinaryExpressionNode n => $"({Render(n.Left, ctx)} {n.Op} {Render(n.Right, ctx)})",
        CastExpressionNode n => Render(n.Expression, ctx),
        DefaultExpressionNode n => DefaultValueRenderer.Render(n, ctx),
        DictionaryLiteralExpressionNode n => RenderDictionary(n, ctx),
        IdentifierExpressionNode n => RenderIdentifier(n, ctx),
        IndexerAccessExpressionNode n => $"{Render(n.Target, ctx)}[{Render(n.Index, ctx)}]",
        InvocationExpressionNode n => $"{Render(n.Target, ctx)}({JoinArgs(n.PositionalArgs, n.NamedArgs, ctx)})",
        LambdaExpressionNode n => RenderLambda(n, ctx),
        LiteralExpressionNode n => RenderLiteral(n),
        MemberAccessExpressionNode n => Emitters.Expressions.ExceptionMemberAccessHelper.TryRender(n, ctx, out var exceptionMember)
            ? exceptionMember
            : Emitters.Expressions.StaticMemberAccessHelper.TryRenderRead(n, ctx, out var staticMember)
            ? staticMember
            : Emitters.Expressions.InstanceMemberAccessHelper.TryRenderRead(n, ctx, out var instanceMember)
                ? instanceMember
                : $"{Render(n.Target, ctx)}.{n.MemberName}",
        NameofExpressionNode n => $"\"{n.Chain.LastOrDefault() ?? string.Empty}\"",
        NullCoalescingExpressionNode n => $"({Render(n.Left, ctx)} != undefined ? {Render(n.Left, ctx)} : {Render(n.Right, ctx)})",
        NullConditionalExpressionNode n => RenderNullConditional(n, ctx),
        ObjectCreationExpressionNode n => ObjectCreationExpressionEmitter.Render(n, ctx),
        TernaryExpressionNode n => $"({Render(n.Condition, ctx)} ? {Render(n.ThenExpr, ctx)} : {Render(n.ElseExpr, ctx)})",
        ThisExpressionNode => ctx.SelfName ?? EmitContext.InstParam,
        TypeofExpressionNode n => RenderTypeof(n, ctx),
        UnaryExpressionNode n => ExpressionFormatHelper.Unary(n, ctx),
        _ => string.Empty
    };

    private static string JoinArgs(IReadOnlyList<IAstNode> positionalArgs, IReadOnlyList<NamedArgNode> namedArgs, EmitContext ctx) =>
        string.Join(", ", positionalArgs.Select(a => Render(a, ctx)).Concat(namedArgs.Select(a => Render(a.Value, ctx))));

    private static string RenderDictionary(DictionaryLiteralExpressionNode node, EmitContext ctx)
    {
        var ctorName = ResolveDictionaryCtorName(ctx);
        var addName = ResolveDictionaryAddName(ctx);
        if (node.Entries.Count == 0)
            return $"{ctorName}()";

        var adds = string.Join(" ", node.Entries.Select(e =>
            $"{addName}(__d, {Render(e.Key, ctx)}, {Render(e.Value, ctx)});"));
        return $"(function() {{ var __d = {ctorName}(); {adds} return __d; }})()";
    }

    private static string ResolveDictionaryCtorName(EmitContext ctx) =>
        ctx.Symbols.TryResolve("Dictionary", 2, ctx.CurrentNamespacePrefix, ["TypedGML.Collections"], out var dictType)
            ? NamingConvention.ConstructorName(dictType)
            : "Dictionary_create";

    private static string ResolveDictionaryAddName(EmitContext ctx) =>
        ctx.Symbols.TryResolve("Dictionary", 2, ctx.CurrentNamespacePrefix, ["TypedGML.Collections"], out var dictType)
            ? NamingConvention.MethodName(dictType, dictType.Members.First(m => m.Kind == Symbols.MemberKind.Method && m.Name == "Add"))
            : "Dictionary_Add";

    private static string RenderLambda(LambdaExpressionNode node, EmitContext ctx)
    {
        var parameters = string.Join(", ", node.Parameters.Select(p => p.Name));
        if (node.Body is not Ast.Statements.BlockStatementNode)
        {
            var rendered = Render(node.Body, ctx);
            return EmitDelegateTypeHelper.TrySignature(ctx.CurrentExpectedType, ctx, out var returnType, out _) && returnType != "void"
                ? $"function({parameters}) {{ return {rendered}; }}"
                : $"function({parameters}) {{ {rendered}; }}";
        }

        var writer = new GmlWriter();
        var nested = ctx.WithWriter(writer);
        writer.Write($"function({parameters})");
        nested.Emitter.Emit(node.Body, nested);
        return writer.GetOutput().TrimEnd();
    }

    private static string RenderNullConditional(NullConditionalExpressionNode node, EmitContext ctx)
    {
        var target = Render(node.Target, ctx);
        var member = NullConditionalExpressionEmitter.RenderMemberRead(node, target, ctx);
        return $"({target} != undefined ? {member} : undefined)";
    }

    private static string RenderIdentifier(IdentifierExpressionNode node, EmitContext ctx) =>
        ctx.TryGetSubstitution(node.Name, out var substitution)
            ? ctx.RenderSubstitution(node.Name, substitution)
            : Emitters.Expressions.StaticMemberAccessHelper.TryRenderRead(node, ctx, out var rendered)
            ? rendered
            : Emitters.Expressions.InstanceMemberAccessHelper.TryRenderRead(node, ctx, out var instanceRendered)
                ? instanceRendered
                : node.Name;

    private static string RenderTypeof(TypeofExpressionNode node, EmitContext ctx) =>
        ctx.Symbols.TryResolve(node.TypeName, ctx.CurrentNamespacePrefix, ctx.UsingPrefixes, out var type)
            ? $"\"{NamingConvention.TypeName(type)}\""
            : $"\"{node.TypeName}\"";

    private static string RenderLiteral(LiteralExpressionNode node) => node.Kind switch
    {
        LiteralKind.String => $"\"{Escape(Unquote(node.Value?.ToString() ?? string.Empty))}\"",
        LiteralKind.Bool => string.Equals(node.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
        LiteralKind.Null => "undefined",
        _ => node.Value?.ToString() ?? "undefined"
    };

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;

}
