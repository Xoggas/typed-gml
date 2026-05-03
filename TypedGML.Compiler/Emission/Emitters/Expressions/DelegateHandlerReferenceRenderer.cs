using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class DelegateHandlerReferenceRenderer
{
    public static string Render(IAstNode handler, EmitContext ctx)
    {
        if (handler is IdentifierExpressionNode identifier &&
            TryResolveCurrentMethod(identifier.Name, ctx, out var owner, out var method))
            return BindToSelf(NamingConvention.MethodName(owner, method), ctx);

        if (handler is MemberAccessExpressionNode access &&
            ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type))
        {
            var member = type.Members.FirstOrDefault(member =>
                member.Kind == MemberKind.Method && member.Name == access.MemberName);
            if (member is not null)
                return $"method({ctx.Emitter.Render(access.Target, ctx)}, {NamingConvention.MethodName(type, member)})";
        }

        return ctx.Emitter.Render(handler, ctx);
    }

    private static string BindToSelf(string functionName, EmitContext ctx) =>
        $"method({ctx.SelfName ?? "self"}, {functionName})";

    private static bool TryResolveCurrentMethod(string name, EmitContext ctx, out TypeSymbol owner, out MemberSymbol method)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            method = current.Members.FirstOrDefault(member => member.Kind == MemberKind.Method && member.Name == name)!;
            if (method is not null)
            {
                owner = current;
                return true;
            }
        }

        owner = null!;
        method = null!;
        return false;
    }
}
