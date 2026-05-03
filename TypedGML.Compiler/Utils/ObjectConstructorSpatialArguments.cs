using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Utils;

internal static class ObjectConstructorSpatialArguments
{
    public static bool SuppliesRequiredValues(MemberSymbol constructor, Func<IAstNode, string?> resolveType) =>
        ParametersSupplyRequiredValues(constructor) || BaseChainSuppliesRequiredValues(constructor, resolveType);

    public static bool TryGetBaseChainValues(
        MemberSymbol? constructor,
        Func<IAstNode, string?> resolveType,
        out IReadOnlyList<IAstNode> values)
    {
        values = [];
        if (constructor is null || !BaseChainSuppliesRequiredValues(constructor, resolveType))
            return false;

        values = constructor.ConstructorChainArgs.Take(3).ToList();
        return true;
    }

    public static bool ParametersSupplyRequiredValues(MemberSymbol constructor) =>
        constructor.Parameters.Count >= 3 &&
        constructor.Parameters[0].TypeRef == "number" &&
        constructor.Parameters[1].TypeRef == "number" &&
        constructor.Parameters[2].TypeRef == "string";

    private static bool BaseChainSuppliesRequiredValues(MemberSymbol constructor, Func<IAstNode, string?> resolveType)
    {
        if (constructor.ConstructorChainTarget != ConstructorChainTarget.Base ||
            constructor.ConstructorChainArgs.Count < 3)
            return false;

        return constructor.ConstructorChainArgs.Take(3).All(arg => arg is LiteralExpressionNode) &&
               resolveType(constructor.ConstructorChainArgs[0]) == "number" &&
               resolveType(constructor.ConstructorChainArgs[1]) == "number" &&
               resolveType(constructor.ConstructorChainArgs[2]) == "string";
    }
}
