using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Utils;

internal static class AstArgumentSubstituter
{
    public static IReadOnlyList<IAstNode> Substitute(
        IReadOnlyList<IAstNode> nodes,
        IReadOnlyDictionary<string, IAstNode> map) =>
        nodes.Select(node => Substitute(node, map)).ToList();

    public static IAstNode Substitute(IAstNode node, IReadOnlyDictionary<string, IAstNode> map) => node switch
    {
        IdentifierExpressionNode identifier when map.TryGetValue(identifier.Name, out var replacement) => replacement,
        ArrayLiteralExpressionNode n => n with { Elements = Substitute(n.Elements, map) },
        AssignmentExpressionNode n => n with { Target = Substitute(n.Target, map), Value = Substitute(n.Value, map) },
        BaseCallExpressionNode n => n with { Args = Substitute(n.Args, map) },
        BinaryExpressionNode n => n with { Left = Substitute(n.Left, map), Right = Substitute(n.Right, map) },
        CastExpressionNode n => n with { Expression = Substitute(n.Expression, map) },
        DictionaryEntryNode n => n with { Key = Substitute(n.Key, map), Value = Substitute(n.Value, map) },
        DictionaryLiteralExpressionNode n => n with { Entries = n.Entries.Select(e => (DictionaryEntryNode)Substitute(e, map)).ToList() },
        IndexerAccessExpressionNode n => n with { Target = Substitute(n.Target, map), Index = Substitute(n.Index, map) },
        InvocationExpressionNode n => n with { Target = Substitute(n.Target, map), PositionalArgs = Substitute(n.PositionalArgs, map), NamedArgs = SubstituteNamed(n.NamedArgs, map) },
        LambdaExpressionNode n => n with { Body = Substitute(n.Body, Remove(map, n.Parameters.Select(p => p.Name))) },
        MemberAccessExpressionNode n => n with { Target = Substitute(n.Target, map) },
        NamedArgNode n => n with { Value = Substitute(n.Value, map) },
        NullCoalescingExpressionNode n => n with { Left = Substitute(n.Left, map), Right = Substitute(n.Right, map) },
        NullConditionalExpressionNode n => n with { Target = Substitute(n.Target, map) },
        ObjectCreationExpressionNode n => n with { PositionalArgs = Substitute(n.PositionalArgs, map), NamedArgs = SubstituteNamed(n.NamedArgs, map) },
        TernaryExpressionNode n => n with { Condition = Substitute(n.Condition, map), ThenExpr = Substitute(n.ThenExpr, map), ElseExpr = Substitute(n.ElseExpr, map) },
        UnaryExpressionNode n => n with { Operand = Substitute(n.Operand, map) },
        _ => node
    };

    private static IReadOnlyList<NamedArgNode> SubstituteNamed(
        IReadOnlyList<NamedArgNode> nodes,
        IReadOnlyDictionary<string, IAstNode> map) =>
        nodes.Select(node => (NamedArgNode)Substitute(node, map)).ToList();

    private static IReadOnlyDictionary<string, IAstNode> Remove(
        IReadOnlyDictionary<string, IAstNode> map,
        IEnumerable<string> names)
    {
        var blocked = names.ToHashSet(StringComparer.Ordinal);
        return map
            .Where(pair => !blocked.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }
}
