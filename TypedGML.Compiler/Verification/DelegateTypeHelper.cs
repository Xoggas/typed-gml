using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class DelegateTypeHelper
{
    public static bool TrySignature(string? typeRef, VerificationContext ctx, out string returnType, out IReadOnlyList<string> parameterTypes)
    {
        var root = TypeReferenceHelper.RootName(typeRef);
        var args = TypeReferenceHelper.TypeArguments(typeRef);
        if (root == "Action")
        {
            returnType = "void";
            parameterTypes = args;
            return true;
        }

        if (root == "Func" && args.Count > 0)
        {
            returnType = args[^1];
            parameterTypes = args.Take(args.Count - 1).ToList();
            return true;
        }

        if (SymbolResolver.TryResolveType(typeRef, ctx, out var symbol) && symbol.Kind == TypeKind.Delegate && symbol.Members.Count > 0)
        {
            var invoke = symbol.Members.First();
            returnType = invoke.ReturnType;
            parameterTypes = invoke.Parameters.Select(parameter => parameter.TypeRef).ToList();
            return true;
        }

        returnType = string.Empty;
        parameterTypes = [];
        return false;
    }
}
