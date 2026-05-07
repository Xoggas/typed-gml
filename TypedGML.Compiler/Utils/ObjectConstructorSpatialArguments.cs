using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Utils;

internal delegate bool ConstructorLookup(
    TypeSymbol type,
    IReadOnlyList<IAstNode> mixedArgs,
    out MemberSymbol? constructor,
    out IReadOnlyList<IAstNode> orderedArgs);

internal static class ObjectConstructorSpatialArguments
{
    private const string GameObjectTypeName = "TypedGML.GameObjects.GameObject";

    public static bool SuppliesRequiredValues(
        TypeSymbol type,
        MemberSymbol? constructor,
        ConstructorLookup lookup) =>
        TryGetValues(type, constructor, lookup, out _);

    public static bool TryGetValues(
        TypeSymbol type,
        MemberSymbol? constructor,
        ConstructorLookup lookup,
        out IReadOnlyList<IAstNode> values) =>
        constructor is null
            ? TryTraceBase(type, [], lookup, new HashSet<string>(StringComparer.Ordinal), out values)
            : TryTrace(type, constructor, lookup, EmptySubstitutions(), new HashSet<string>(StringComparer.Ordinal), out values);

    public static bool ParametersSupplyRequiredValues(MemberSymbol constructor) =>
        constructor.Parameters.Count >= 3 &&
        constructor.Parameters[0].TypeRef == "number" &&
        constructor.Parameters[1].TypeRef == "number" &&
        constructor.Parameters[2].TypeRef == "string";

    private static bool TryTrace(
        TypeSymbol type,
        MemberSymbol constructor,
        ConstructorLookup lookup,
        IReadOnlyDictionary<string, IAstNode> substitutions,
        ISet<string> visited,
        out IReadOnlyList<IAstNode> values)
    {
        values = [];
        if (!visited.Add(Signature(type, constructor)))
            return false;

        var args = AstArgumentSubstituter.Substitute(constructor.ConstructorChainArgs, substitutions);
        return constructor.ConstructorChainTarget switch
        {
            ConstructorChainTarget.Base => TryTraceBase(type, args, lookup, visited, out values),
            ConstructorChainTarget.This => TryTraceThis(type, args, lookup, visited, out values),
            _ => TryTraceBase(type, [], lookup, visited, out values)
        };
    }

    private static bool TryTraceBase(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        ConstructorLookup lookup,
        ISet<string> visited,
        out IReadOnlyList<IAstNode> values)
    {
        values = [];
        if (type.Base is null)
            return false;

        if (!lookup(type.Base, args, out var constructor, out var orderedArgs))
            return false;

        if (type.Base.QualifiedName == GameObjectTypeName)
        {
            values = orderedArgs.Take(3).ToList();
            return values.Count == 3;
        }

        return constructor is not null &&
            TryTrace(type.Base, constructor, lookup, ParameterMap(constructor, orderedArgs), visited, out values);
    }

    private static bool TryTraceThis(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        ConstructorLookup lookup,
        ISet<string> visited,
        out IReadOnlyList<IAstNode> values)
    {
        values = [];
        return lookup(type, args, out var constructor, out var orderedArgs) &&
            constructor is not null &&
            TryTrace(type, constructor, lookup, ParameterMap(constructor, orderedArgs), visited, out values);
    }

    private static IReadOnlyDictionary<string, IAstNode> ParameterMap(
        MemberSymbol constructor,
        IReadOnlyList<IAstNode> orderedArgs) =>
        constructor.Parameters
            .Select((parameter, index) => new { parameter.Name, Value = ArgumentValue(parameter, index, orderedArgs) })
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Name, pair => pair.Value!, StringComparer.Ordinal);

    private static IAstNode? ArgumentValue(ParameterSymbol parameter, int index, IReadOnlyList<IAstNode> args) =>
        index < args.Count ? args[index] : parameter.DefaultValue as IAstNode;

    private static IReadOnlyDictionary<string, IAstNode> EmptySubstitutions() =>
        new Dictionary<string, IAstNode>(StringComparer.Ordinal);

    private static string Signature(TypeSymbol type, MemberSymbol constructor) =>
        $"{type.QualifiedName}({string.Join(",", constructor.Parameters.Select(p => p.TypeRef))})";
}
