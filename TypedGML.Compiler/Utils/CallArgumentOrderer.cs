using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Utils;

internal static class CallArgumentOrderer
{
    public static IReadOnlyList<IAstNode> PositionalFromMixed(IEnumerable<IAstNode> args) =>
        args.Where(arg => arg is not NamedArgNode).ToList();

    public static IReadOnlyList<NamedArgNode> NamedFromMixed(IEnumerable<IAstNode> args) =>
        args.OfType<NamedArgNode>().ToList();

    public static bool HasDuplicateNamedArgs(IEnumerable<NamedArgNode> namedArgs) =>
        namedArgs.GroupBy(arg => arg.Name, StringComparer.Ordinal).Any(group => group.Count() > 1);

    public static bool TryBind(
        MemberSymbol member,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        out IReadOnlyList<(int ParameterIndex, IAstNode Value)> bindings)
    {
        bindings = [];
        if (!TryBuildValues(member, positionalArgs, namedArgs, out var values))
            return false;

        bindings = values
            .Select((value, index) => (Index: index, Value: value))
            .Where(pair => pair.Value is not null)
            .Select(pair => (pair.Index, pair.Value!))
            .ToList();
        return true;
    }

    public static bool TryOrder(
        MemberSymbol member,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        bool includeDefaults,
        out IReadOnlyList<IAstNode> orderedArgs)
    {
        orderedArgs = [];
        if (!TryBuildValues(member, positionalArgs, namedArgs, out var values))
            return false;

        if (includeDefaults)
            for (var i = 0; i < values.Length; i++)
                if (values[i] is null && member.Parameters[i].DefaultValue is IAstNode defaultValue)
                    values[i] = defaultValue;

        orderedArgs = values.Where(value => value is not null).Cast<IAstNode>().ToList();
        return true;
    }

    private static bool TryBuildValues(
        MemberSymbol member,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        out IAstNode?[] values)
    {
        values = [];
        var suppliedCount = positionalArgs.Count + namedArgs.Count;
        if (positionalArgs.Count > member.Parameters.Count ||
            suppliedCount < member.Parameters.Count(parameter => !parameter.HasDefault) ||
            suppliedCount > member.Parameters.Count ||
            HasDuplicateNamedArgs(namedArgs))
            return false;

        values = new IAstNode?[member.Parameters.Count];
        for (var i = 0; i < positionalArgs.Count; i++)
            values[i] = positionalArgs[i];

        foreach (var namedArg in namedArgs)
        {
            var index = IndexOf(member, namedArg.Name);
            if (index < 0 || index < positionalArgs.Count || values[index] is not null)
                return false;
            values[index] = namedArg.Value;
        }

        return true;
    }

    public static int IndexOf(MemberSymbol member, string name)
    {
        for (var i = 0; i < member.Parameters.Count; i++)
            if (member.Parameters[i].Name == name)
                return i;

        return -1;
    }
}
