using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class ListLiteralRenderer
{
    public static bool TryRender(ArrayLiteralExpressionNode expression, EmitContext ctx, out string rendered)
    {
        rendered = string.Empty;
        if (!TryResolveListTarget(ctx.CurrentExpectedType, ctx, out var listType, out var elementType))
            return false;

        var ctorName = NamingConvention.ConstructorName(listType);
        if (expression.Elements.Count == 0)
        {
            rendered = $"{ctorName}()";
            return true;
        }

        var addName = NamingConvention.MethodName(listType, listType.Members.First(m => m.Kind == MemberKind.Method && m.Name == "Add"));
        var adds = string.Join(" ", expression.Elements.Select(element =>
            $"{addName}(__l, {ctx.RenderWithExpected(element, elementType)});"));
        rendered = $"(function() {{ var __l = {ctorName}(); {adds} return __l; }})()";
        return true;
    }

    private static bool TryResolveListTarget(
        string? typeRef,
        EmitContext ctx,
        out TypeSymbol listType,
        out string elementType)
    {
        listType = null!;
        elementType = string.Empty;
        if (string.IsNullOrWhiteSpace(typeRef) ||
            !ExpressionSymbolHelper.TryResolveType(ctx, typeRef, out var resolved) ||
            resolved.Arity != 1 ||
            ShortName(resolved) != "List")
            return false;

        var typeArgs = TypeReferenceHelper.TypeArguments(typeRef);
        if (typeArgs.Count != 1)
            return false;

        listType = resolved;
        elementType = typeArgs[0];
        return true;
    }

    private static string ShortName(TypeSymbol type)
    {
        var index = type.QualifiedName.LastIndexOf('.');
        return index < 0 ? type.QualifiedName : type.QualifiedName[(index + 1)..];
    }
}
