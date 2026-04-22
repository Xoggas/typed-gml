using System.Globalization;

namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlFuncCallExpr : TgmlExpression
{
    public required string FunctionName { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlParenExpr : TgmlExpression
{
    public required TgmlExpression Inner { get; init; }
}

public sealed class TgmlIdExpr : TgmlExpression
{
    public required string Name { get; init; }
}

public sealed class TgmlNullExpr : TgmlExpression { }
public sealed class TgmlFieldKeywordExpr : TgmlExpression { }
public sealed class TgmlValueKeywordExpr : TgmlExpression { }

public sealed class TgmlBoolLiteralExpr : TgmlExpression
{
    public required bool Value { get; init; }
}

public sealed class TgmlIntLiteralExpr : TgmlExpression
{
    public required string RawValue { get; init; }

    public long ParsedValue =>
        RawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt64(RawValue[2..], 16)
            : RawValue.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(RawValue[2..], 2)
                : long.Parse(RawValue);
}

public sealed class TgmlRealLiteralExpr : TgmlExpression
{
    public required string RawValue { get; init; }
    public double ParsedValue => double.Parse(RawValue, CultureInfo.InvariantCulture);
}

public sealed class TgmlStringLiteralExpr : TgmlExpression
{
    public required string RawValue { get; init; }
    public string Value => RawValue.Length >= 2 ? RawValue[1..^1] : string.Empty;
}
