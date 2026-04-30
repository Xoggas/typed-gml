using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Emission;

internal static class EmitDelegateTypeHelper
{
    public static bool TrySignature(string? typeRef, EmitContext ctx, out string returnType, out IReadOnlyList<string> parameterTypes)
    {
        var root = TypeReferenceHelper.RootName(typeRef);
        var args = TypeReferenceHelper.TypeArguments(typeRef);
        if (root is "Action" or "TypedGML.Action")
        {
            returnType = "void";
            parameterTypes = args;
            return true;
        }

        if (root is "Func" or "TypedGML.Func" && args.Count > 0)
        {
            returnType = args[^1];
            parameterTypes = args.Take(args.Count - 1).ToList();
            return true;
        }

        if (ExpressionSymbolHelper.TryResolveType(ctx, typeRef, out var symbol) &&
            symbol.Kind == TypeKind.Delegate &&
            symbol.Members.Count > 0)
        {
            var invoke = symbol.Members[0];
            returnType = invoke.ReturnType;
            parameterTypes = invoke.Parameters.Select(parameter => parameter.TypeRef).ToList();
            return true;
        }

        returnType = string.Empty;
        parameterTypes = [];
        return false;
    }
}
