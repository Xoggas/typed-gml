using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ExceptionMemberAccessHelper
{
    public static bool TryRender(MemberAccessExpressionNode access, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!ExceptionNavigation.TryGetNativeField(access.MemberName, out var fieldName) ||
            !ExpressionSymbolHelper.TryResolveTargetType(access.Target, ctx, out var type) ||
            !ExceptionNavigation.IsExceptionType(type))
            return false;

        rendered = $"{ctx.Emitter.Render(access.Target, ctx)}.{fieldName}";
        return true;
    }
}
