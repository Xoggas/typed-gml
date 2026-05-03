using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorParameterSubstituter
{
    public static IAstNode Substitute(
        IAstNode node,
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlyList<IAstNode> arguments) =>
        Replace(node, Map(parameters, arguments));

    public static IAstNode Substitute(IAstNode node, IReadOnlyDictionary<string, IAstNode> map) =>
        Replace(node, map);

    public static IReadOnlyList<IAstNode> Substitute(
        IReadOnlyList<IAstNode> nodes,
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlyList<IAstNode> arguments)
    {
        var map = Map(parameters, arguments);
        return nodes.Select(node => Replace(node, map)).ToList();
    }

    public static IReadOnlyList<IAstNode> Substitute(
        IReadOnlyList<IAstNode> nodes,
        IReadOnlyDictionary<string, IAstNode> map) =>
        nodes.Select(node => Replace(node, map)).ToList();

    public static IReadOnlyDictionary<string, IAstNode> CreateTempMap(
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlyList<IAstNode> arguments,
        Func<string> nextTempName,
        out IReadOnlyList<ConstructorArgumentTemp> temps) =>
        MapWithTemps(parameters, arguments, nextTempName, out temps);

    private static IReadOnlyDictionary<string, IAstNode> Map(
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlyList<IAstNode> arguments) =>
        parameters
            .Select((parameter, index) => new { parameter.Name, Value = ArgumentValue(parameter, index, arguments) })
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Name, pair => pair.Value!, StringComparer.Ordinal);

    private static IAstNode? ArgumentValue(ParameterNode parameter, int index, IReadOnlyList<IAstNode> arguments) =>
        index < arguments.Count ? arguments[index] : parameter.DefaultValue;

    private static IReadOnlyDictionary<string, IAstNode> MapWithTemps(
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlyList<IAstNode> arguments,
        Func<string> nextTempName,
        out IReadOnlyList<ConstructorArgumentTemp> temps)
    {
        var result = new Dictionary<string, IAstNode>(StringComparer.Ordinal);
        var tempList = new List<ConstructorArgumentTemp>();
        for (var i = 0; i < parameters.Count; i++)
        {
            var value = ArgumentValue(parameters[i], i, arguments);
            if (value is null)
                continue;

            if (IsTrivial(value))
            {
                result[parameters[i].Name] = value;
                continue;
            }

            var tempName = nextTempName();
            tempList.Add(new ConstructorArgumentTemp(tempName, parameters[i], value));
            result[parameters[i].Name] = new IdentifierExpressionNode(tempName, value.Location);
        }

        temps = tempList;
        return result;
    }

    private static bool IsTrivial(IAstNode node) =>
        node is IdentifierExpressionNode or LiteralExpressionNode;

    private static IAstNode Replace(IAstNode node, IReadOnlyDictionary<string, IAstNode> map) => node switch
    {
        IdentifierExpressionNode identifier when map.TryGetValue(identifier.Name, out var replacement) => replacement,
        ArrayLiteralExpressionNode n => n with { Elements = ReplaceMany(n.Elements, map) },
        AssignmentExpressionNode n => n with { Target = Replace(n.Target, map), Value = Replace(n.Value, map) },
        BaseCallExpressionNode n => n with { Args = ReplaceMany(n.Args, map) },
        BinaryExpressionNode n => n with { Left = Replace(n.Left, map), Right = Replace(n.Right, map) },
        CastExpressionNode n => n with { Expression = Replace(n.Expression, map) },
        DictionaryEntryNode n => n with { Key = Replace(n.Key, map), Value = Replace(n.Value, map) },
        DictionaryLiteralExpressionNode n => n with { Entries = n.Entries.Select(e => (DictionaryEntryNode)Replace(e, map)).ToList() },
        IndexerAccessExpressionNode n => n with { Target = Replace(n.Target, map), Index = Replace(n.Index, map) },
        InvocationExpressionNode n => n with { Target = Replace(n.Target, map), PositionalArgs = ReplaceMany(n.PositionalArgs, map), NamedArgs = ReplaceNamed(n.NamedArgs, map) },
        LambdaExpressionNode n => n with { Body = Replace(n.Body, Remove(map, n.Parameters.Select(p => p.Name))) },
        MemberAccessExpressionNode n => n with { Target = Replace(n.Target, map) },
        NamedArgNode n => n with { Value = Replace(n.Value, map) },
        NullCoalescingExpressionNode n => n with { Left = Replace(n.Left, map), Right = Replace(n.Right, map) },
        NullConditionalExpressionNode n => n with { Target = Replace(n.Target, map) },
        ObjectCreationExpressionNode n => n with { PositionalArgs = ReplaceMany(n.PositionalArgs, map), NamedArgs = ReplaceNamed(n.NamedArgs, map) },
        TernaryExpressionNode n => n with { Condition = Replace(n.Condition, map), ThenExpr = Replace(n.ThenExpr, map), ElseExpr = Replace(n.ElseExpr, map) },
        UnaryExpressionNode n => n with { Operand = Replace(n.Operand, map) },
        BlockStatementNode n => n with { Statements = ReplaceMany(n.Statements, map) },
        ElseIfClauseNode n => n with { Condition = Replace(n.Condition, map), ThenBlock = Replace(n.ThenBlock, map) },
        ExpressionStatementNode n => n with { Expression = Replace(n.Expression, map) },
        ForStatementNode n => n with { Init = ReplaceOptional(n.Init, map), Condition = ReplaceOptional(n.Condition, map), Update = ReplaceMany(n.Update, map), Body = Replace(n.Body, map) },
        IfStatementNode n => n with { Condition = Replace(n.Condition, map), ThenBlock = Replace(n.ThenBlock, map), ElseIfClauses = n.ElseIfClauses.Select(e => (ElseIfClauseNode)Replace(e, map)).ToList(), ElseBlock = ReplaceOptional(n.ElseBlock, map) },
        RepeatStatementNode n => n with { Count = Replace(n.Count, map), Body = Replace(n.Body, map) },
        ReturnStatementNode n => n with { Value = ReplaceOptional(n.Value, map) },
        SwitchSectionNode n => n with { Label = ReplaceOptional(n.Label, map), Statements = ReplaceMany(n.Statements, map) },
        SwitchStatementNode n => n with { Value = Replace(n.Value, map), Sections = n.Sections.Select(s => (SwitchSectionNode)Replace(s, map)).ToList() },
        ThrowStatementNode n => n with { Expression = Replace(n.Expression, map) },
        TryStatementNode n => n with { TryBlock = Replace(n.TryBlock, map), CatchClauses = ReplaceCatchClauses(n.CatchClauses, map), FinallyBlock = ReplaceOptional(n.FinallyBlock, map) },
        VarDeclarationStatementNode n => n with { Initializer = ReplaceOptional(n.Initializer, Remove(map, [n.Name])) },
        WhileStatementNode n => n with { Condition = Replace(n.Condition, map), Body = Replace(n.Body, map) },
        WithStatementNode n => n with { Target = Replace(n.Target, map), Body = Replace(n.Body, map) },
        _ => node
    };

    private static IReadOnlyList<IAstNode> ReplaceMany(IEnumerable<IAstNode> nodes, IReadOnlyDictionary<string, IAstNode> map) =>
        nodes.Select(node => Replace(node, map)).ToList();

    private static IAstNode? ReplaceOptional(IAstNode? node, IReadOnlyDictionary<string, IAstNode> map) =>
        node is null ? null : Replace(node, map);

    private static IReadOnlyList<NamedArgNode> ReplaceNamed(IEnumerable<NamedArgNode> nodes, IReadOnlyDictionary<string, IAstNode> map) =>
        nodes.Select(node => (NamedArgNode)Replace(node, map)).ToList();

    private static IReadOnlyList<CatchClauseNode> ReplaceCatchClauses(IEnumerable<CatchClauseNode> clauses, IReadOnlyDictionary<string, IAstNode> map) =>
        clauses.Select(clause => clause with { Body = Replace(clause.Body, Remove(map, [clause.VariableName])) }).ToList();

    private static IReadOnlyDictionary<string, IAstNode> Remove(IReadOnlyDictionary<string, IAstNode> map, IEnumerable<string> names)
    {
        var blocked = names.ToHashSet(StringComparer.Ordinal);
        return map.Where(pair => !blocked.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }
}
