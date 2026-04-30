using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class InstanceMemberAccessHelper
{
    public static bool TryRenderRead(IAstNode node, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!TryResolve(node, ctx, out var owner, out var member) || IsStaticLike(member))
            return false;

        rendered = node switch
        {
            IdentifierExpressionNode => RenderCurrentMemberRead(owner, member, ctx),
            MemberAccessExpressionNode access => RenderTargetMemberRead(access, owner, member, ctx),
            _ => string.Empty
        };
        return !string.IsNullOrEmpty(rendered);
    }

    public static bool TryRenderAssignment(AssignmentExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!TryResolve(expression.Target, ctx, out var owner, out var member) || IsStaticLike(member))
            return false;

        var value = ctx.RenderWithExpected(expression.Value, member.ReturnType);
        if (member.Kind == MemberKind.Field)
        {
            var target = expression.Target is IdentifierExpressionNode
                ? CurrentField(member, ctx)
                : $"{ctx.Emitter.Render(((MemberAccessExpressionNode)expression.Target).Target, ctx)}.{member.Name}";
            rendered = $"{target} {expression.Op} {value}";
            return true;
        }

        if (member.Kind != MemberKind.Property)
            return false;

        rendered = PropertyAssignmentRenderer.Render(expression, owner, member, value, ctx);
        return !string.IsNullOrEmpty(rendered);
    }

    private static string RenderCurrentMemberRead(TypeSymbol owner, MemberSymbol member, EmitContext ctx) => member.Kind switch
    {
        MemberKind.Field => CurrentField(member, ctx),
        MemberKind.Property => CurrentPropertyRead(owner, member, ctx),
        _ => string.Empty
    };

    private static string RenderTargetMemberRead(MemberAccessExpressionNode access, TypeSymbol owner, MemberSymbol member, EmitContext ctx) => member.Kind switch
    {
        MemberKind.Field => $"{ctx.Emitter.Render(access.Target, ctx)}.{member.Name}",
        MemberKind.Property when member.NativePropertyName is not null => $"{ctx.Emitter.Render(access.Target, ctx)}.{member.NativePropertyName}",
        MemberKind.Property => $"{NamingConvention.PropertyGetter(owner, member)}({ctx.Emitter.Render(access.Target, ctx)})",
        _ => string.Empty
    };

    private static string CurrentField(MemberSymbol member, EmitContext ctx) =>
        ctx.IsObjectEventContext || string.IsNullOrEmpty(ctx.SelfName) ? member.Name : $"{ctx.SelfName}.{member.Name}";

    private static string CurrentPropertyRead(TypeSymbol owner, MemberSymbol member, EmitContext ctx)
    {
        if (ctx.IsObjectEventContext && member.NativePropertyName is not null)
            return member.NativePropertyName;
        if (member.NativePropertyName is not null && ctx.SelfName is not null)
            return $"{ctx.SelfName}.{member.NativePropertyName}";
        return $"{NamingConvention.PropertyGetter(owner, member)}({ctx.SelfName})";
    }

    private static bool TryResolve(IAstNode node, EmitContext ctx, out TypeSymbol owner, out MemberSymbol member)
    {
        switch (node)
        {
            case IdentifierExpressionNode identifier when !ctx.Scope.TryResolve(identifier.Name, out _):
                return TryResolveCurrent(identifier.Name, ctx, out owner, out member);
            case MemberAccessExpressionNode access when ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type):
                member = type.Members.FirstOrDefault(m => m.Name == access.MemberName)!;
                owner = type;
                return member is not null;
            default:
                owner = null!;
                member = null!;
                return false;
        }
    }

    private static bool TryResolveCurrent(string name, EmitContext ctx, out TypeSymbol owner, out MemberSymbol member)
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

    private static bool IsStaticLike(MemberSymbol member) =>
        member.Modifiers.Contains("static", StringComparer.Ordinal) ||
        member.Modifiers.Contains("const", StringComparer.Ordinal);
}
