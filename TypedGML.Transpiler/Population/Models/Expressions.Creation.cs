namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlNewObjectExpr : TgmlExpression
{
    public required TgmlTypeRef Type { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlNewArrayExpr : TgmlExpression
{
    public required TgmlTypeRef ElementType { get; init; }
    public required TgmlExpression Size { get; init; }
}

public sealed class TgmlNewImplicitExpr : TgmlExpression
{
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlTypeofExpr : TgmlExpression
{
    public required TgmlTypeRef Type { get; init; }
}

public sealed class TgmlNameofExpr : TgmlExpression
{
    public required TgmlExpression Operand { get; init; }
}

public sealed class TgmlDefaultExpr : TgmlExpression
{
    public TgmlTypeRef? Type { get; init; }
}

public sealed class TgmlBaseCallExpr : TgmlExpression
{
    public required string MethodName { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlBaseAccessExpr : TgmlExpression
{
    public required string MemberName { get; init; }
}

public sealed class TgmlLambdaExpr : TgmlExpression
{
    public List<TgmlParam> Params { get; init; } = new();
    public TgmlExpression? ExprBody { get; init; }
    public TgmlBlock? BlockBody { get; init; }
}

public sealed class TgmlArrayInitExpr : TgmlExpression
{
    public List<TgmlExpression> Elements { get; init; } = new();
}

public sealed class TgmlDictionaryEntry
{
    public required TgmlExpression Key { get; init; }
    public required TgmlExpression Value { get; init; }
}

public sealed class TgmlDictionaryInitExpr : TgmlExpression
{
    public List<TgmlDictionaryEntry> Entries { get; init; } = new();
}
