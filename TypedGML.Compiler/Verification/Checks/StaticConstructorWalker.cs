using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Verification.Checks;

internal static class StaticConstructorWalker
{
    public static void CheckBody(IAstNode body, VerificationContext ctx) => InScope(ctx, () => Walk(body, ctx, true));

    public static void CheckInitializer(IAstNode initializer, VerificationContext ctx) => InScope(ctx, () => Walk(initializer, ctx, false));

    private static void Walk(IAstNode node, VerificationContext ctx, bool checkInstanceMembers)
    {
        switch (node)
        {
            case IdentifierExpressionNode identifier:
                if (checkInstanceMembers)
                    StaticReferenceResolver.CheckIdentifier(identifier, ctx);
                break;
            case MemberAccessExpressionNode access:
                StaticReferenceResolver.CheckMemberAccess(access, ctx, checkInstanceMembers);
                break;
            case BlockStatementNode block:
                InScope(ctx, () => block.Statements.ToList().ForEach(statement => Walk(statement, ctx, checkInstanceMembers)));
                return;
            case VarDeclarationStatementNode declaration:
                if (declaration.Initializer is not null)
                    Walk(declaration.Initializer, ctx, checkInstanceMembers);
                ctx.Scope.Declare(declaration.Name, declaration.TypeRef ?? ExpressionTypeResolver.Resolve(declaration.Initializer, ctx) ?? string.Empty);
                return;
            case CatchClauseNode clause:
                CatchScopeHelper.Walk(clause, ctx, body => Walk(body, ctx, checkInstanceMembers));
                return;
            case ForStatementNode loop:
                InScope(ctx, () =>
                {
                    if (loop.Init is not null)
                        Walk(loop.Init, ctx, checkInstanceMembers);
                    if (loop.Condition is not null)
                        Walk(loop.Condition, ctx, checkInstanceMembers);
                    loop.Update.ToList().ForEach(update => Walk(update, ctx, checkInstanceMembers));
                    Walk(loop.Body, ctx, checkInstanceMembers);
                });
                return;
            case LambdaExpressionNode lambda:
                InScope(ctx, () =>
                {
                    foreach (var parameter in lambda.Parameters)
                        ctx.Scope.Declare(parameter.Name, parameter.TypeRef);
                    Walk(lambda.Body, ctx, checkInstanceMembers);
                });
                return;
        }

        foreach (var child in Children(node))
            Walk(child, ctx, checkInstanceMembers);
    }

    private static void InScope(VerificationContext ctx, Action action)
    {
        ctx.Scope.Push();
        action();
        ctx.Scope.Pop();
    }

    private static IEnumerable<IAstNode> Children(IAstNode node) => node switch
    {
        ExpressionStatementNode n => [n.Expression],
        AssignmentExpressionNode n => [n.Target, n.Value],
        BinaryExpressionNode n => [n.Left, n.Right],
        UnaryExpressionNode n => [n.Operand],
        TernaryExpressionNode n => [n.Condition, n.ThenExpr, n.ElseExpr],
        MemberAccessExpressionNode n => [n.Target],
        IndexerAccessExpressionNode n => [n.Target, n.Index],
        InvocationExpressionNode n => [n.Target, .. n.PositionalArgs, .. n.NamedArgs],
        NamedArgNode n => [n.Value],
        ReturnStatementNode n when n.Value is not null => [n.Value],
        IfStatementNode n => [n.Condition, n.ThenBlock, .. n.ElseIfClauses, .. (n.ElseBlock is null ? [] : new[] { n.ElseBlock })],
        ElseIfClauseNode n => [n.Condition, n.ThenBlock],
        WhileStatementNode n => [n.Condition, n.Body],
        RepeatStatementNode n => [n.Count, n.Body],
        WithStatementNode n => [n.Target, n.Body],
        SwitchStatementNode n => [n.Value, .. n.Sections],
        SwitchSectionNode n => [.. (n.Label is null ? [] : new[] { n.Label }), .. n.Statements],
        ThrowStatementNode n => [n.Expression],
        TryStatementNode n => [n.TryBlock, .. n.CatchClauses, .. (n.FinallyBlock is null ? [] : new[] { n.FinallyBlock })],
        CatchClauseNode n => [n.Body],
        ArrayLiteralExpressionNode n => n.Elements,
        DictionaryLiteralExpressionNode n => n.Entries,
        DictionaryEntryNode n => [n.Key, n.Value],
        CastExpressionNode n => [n.Expression],
        NullCoalescingExpressionNode n => [n.Left, n.Right],
        NullConditionalExpressionNode n => [n.Target],
        ObjectCreationExpressionNode n => [.. n.PositionalArgs, .. n.NamedArgs],
        _ => []
    };
}

internal static class StaticReferenceResolver
{
    public static void CheckIdentifier(IdentifierExpressionNode identifier, VerificationContext ctx)
    {
        if (identifier.Name == "this")
        {
            ctx.Diagnostics.Report(DiagnosticCode.InvalidStaticConstructorUsage, DiagnosticSeverity.Error, "Static constructors cannot reference 'this'.", identifier.Location);
            return;
        }

        if (!TryResolveReferencedMember(identifier, ctx, out var member, out var owner, out _) ||
            owner != ctx.CurrentType ||
            member.Modifiers.Contains("static", StringComparer.Ordinal))
            return;

        ctx.Diagnostics.Report(DiagnosticCode.InvalidStaticConstructorUsage, DiagnosticSeverity.Error, $"Static constructors cannot reference instance member '{identifier.Name}'.", identifier.Location);
    }

    public static void CheckMemberAccess(MemberAccessExpressionNode access, VerificationContext ctx, bool checkInstanceMembers)
    {
        if (IsCrossTypeStatic(access.Target, ctx) || IsTypeQualifiedCrossTypeStatic(access, ctx))
            ctx.Diagnostics.Report(DiagnosticCode.CrossTypeStaticReference, DiagnosticSeverity.Error, "Static initialization cannot reference static members from a different type.", access.Location);

        if (!checkInstanceMembers ||
            !TryResolveReferencedMember(access, ctx, out var member, out var owner, out _) ||
            owner != ctx.CurrentType ||
            member.Modifiers.Contains("static", StringComparer.Ordinal))
            return;

        ctx.Diagnostics.Report(DiagnosticCode.InvalidStaticConstructorUsage, DiagnosticSeverity.Error, $"Static constructors cannot reference instance member '{access.MemberName}'.", access.Location);
    }

    private static bool IsCrossTypeStatic(IAstNode node, VerificationContext ctx) =>
        TryResolveReferencedMember(node, ctx, out var member, out var owner, out _) &&
        owner != ctx.CurrentType &&
        member.Modifiers.Contains("static", StringComparer.Ordinal);

    private static bool IsTypeQualifiedCrossTypeStatic(MemberAccessExpressionNode access, VerificationContext ctx) =>
        TryResolveReferencedMember(access, ctx, out var member, out var owner, out var targetIsType) &&
        targetIsType &&
        owner != ctx.CurrentType &&
        member.Modifiers.Contains("static", StringComparer.Ordinal);

    private static bool TryResolveReferencedMember(IAstNode node, VerificationContext ctx, out MemberSymbol member, out TypeSymbol owner, out bool targetIsType)
    {
        targetIsType = false;
        switch (node)
        {
            case IdentifierExpressionNode identifier when identifier.Name is not "this" and not "base" && !ctx.Scope.TryResolve(identifier.Name, out _):
                return TryResolveMember(ctx.CurrentType, identifier.Name, out member, out owner);
            case MemberAccessExpressionNode access when TryResolveTypeChain(access.Target, ctx, out var type):
                targetIsType = true;
                return TryResolveMember(type, access.MemberName, out member, out owner);
            case MemberAccessExpressionNode access when SymbolResolver.TryResolveType(ExpressionTypeResolver.Resolve(access.Target, ctx), ctx, out var targetType):
                return TryResolveMember(targetType, access.MemberName, out member, out owner);
            default:
                member = null!;
                owner = null!;
                return false;
        }
    }

    private static bool TryResolveTypeChain(IAstNode node, VerificationContext ctx, out TypeSymbol type)
    {
        if (Chain(node) is { } typeName && SymbolResolver.TryResolveType(typeName, ctx, out type))
            return true;

        type = null!;
        return false;
    }

    private static string? Chain(IAstNode node) => node switch
    {
        IdentifierExpressionNode identifier => identifier.Name,
        MemberAccessExpressionNode access when Chain(access.Target) is { } prefix => $"{prefix}.{access.MemberName}",
        _ => null
    };

    private static bool TryResolveMember(TypeSymbol? type, string name, out MemberSymbol member, out TypeSymbol owner)
    {
        member = SymbolResolver.FindMember(type, name, out var resolvedOwner)!;
        owner = resolvedOwner!;
        return member is not null && owner is not null;
    }
}
