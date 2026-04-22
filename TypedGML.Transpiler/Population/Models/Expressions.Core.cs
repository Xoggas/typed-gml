namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlArgument
{
    public string? Name { get; init; }
    public required TgmlExpression Value { get; init; }
}

public abstract class TgmlExpression
{
    public int Line { get; init; }
    public int Column { get; init; }
    public Dictionary<string, object> Metadata { get; } = new();
}

public sealed class TgmlMethodCallExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }
    public required string MethodName { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlFieldAccessExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }
    public required string FieldName { get; init; }
}

public sealed class TgmlIndexExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }
    public required TgmlExpression Index { get; init; }
}

public sealed class TgmlInvokeExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlUnaryExpr : TgmlExpression
{
    public required string Operator { get; init; }
    public required TgmlExpression Operand { get; init; }
}

public sealed class TgmlCastExpr : TgmlExpression
{
    public required TgmlTypeRef TargetType { get; init; }
    public required TgmlExpression Operand { get; init; }
}

public sealed class TgmlBinaryExpr : TgmlExpression
{
    public required TgmlExpression Left { get; init; }
    public required string Operator { get; init; }
    public required TgmlExpression Right { get; init; }
}

public sealed class TgmlIsExpr : TgmlExpression
{
    public required TgmlExpression Operand { get; init; }
    public required TgmlTypeRef CheckType { get; init; }
}

public sealed class TgmlAsExpr : TgmlExpression
{
    public required TgmlExpression Operand { get; init; }
    public required TgmlTypeRef TargetType { get; init; }
}

public sealed class TgmlTernaryExpr : TgmlExpression
{
    public required TgmlExpression Condition { get; init; }
    public required TgmlExpression ThenExpr { get; init; }
    public required TgmlExpression ElseExpr { get; init; }
}

public sealed class TgmlAssignExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }
    public required string Operator { get; init; }
    public required TgmlExpression Value { get; init; }
}
