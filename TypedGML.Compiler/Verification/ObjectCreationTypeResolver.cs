using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Verification;

internal static class ObjectCreationTypeResolver
{
    public static string Resolve(ObjectCreationExpressionNode creation, VerificationContext ctx)
    {
        var root = SymbolResolver.TryResolveType(creation.TypeRef, creation.TypeArgs.Count, ctx, out var type)
            ? type.QualifiedName
            : creation.TypeRef;

        return creation.TypeArgs.Count == 0 ? root : $"{root}<{string.Join(", ", creation.TypeArgs)}>";
    }
}
