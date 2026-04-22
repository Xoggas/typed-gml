using System.Globalization;

namespace TypedGML.Transpiler.Population.Models;

// ── All expression node types ────────────────────────────────────────────────

public sealed class TgmlArgument
{
    public string? Name { get; init; }
    public required TgmlExpression Value { get; init; }
}

/// <summary>Base class for all TypedGML expression nodes.</summary>
public abstract class TgmlExpression
{
    public int Line { get; init; }
    public int Column { get; init; }
    public Dictionary<string, object> Metadata { get; } = new();
}

// ── Postfix ──────────────────────────────────────────────────────────────────

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

// ── Unary ─────────────────────────────────────────────────────────────────────

public sealed class TgmlUnaryExpr : TgmlExpression
{
    /// <summary>"-", "~", "not"</summary>
    public required string Operator { get; init; }

    public required TgmlExpression Operand { get; init; }
}

// ── Cast ──────────────────────────────────────────────────────────────────────

public sealed class TgmlCastExpr : TgmlExpression
{
    public required TgmlTypeRef TargetType { get; init; }
    public required TgmlExpression Operand { get; init; }
}

// ── Binary ────────────────────────────────────────────────────────────────────

/// <summary>
///     Covers: arithmetic (+,-,*,/,%), bitwise (&,|,^,&lt;&lt;,>>),
///     comparison (==,!=,&lt;,>,&lt;=,>=), logical (and, or).
/// </summary>
public sealed class TgmlBinaryExpr : TgmlExpression
{
    public required TgmlExpression Left { get; init; }
    public required string Operator { get; init; }
    public required TgmlExpression Right { get; init; }
}

// ── Type tests ────────────────────────────────────────────────────────────────

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

// ── Ternary ───────────────────────────────────────────────────────────────────

public sealed class TgmlTernaryExpr : TgmlExpression
{
    public required TgmlExpression Condition { get; init; }
    public required TgmlExpression ThenExpr { get; init; }
    public required TgmlExpression ElseExpr { get; init; }
}

// ── Assignment ────────────────────────────────────────────────────────────────

public sealed class TgmlAssignExpr : TgmlExpression
{
    public required TgmlExpression Target { get; init; }

    /// <summary>"=", "+=", "-=", "*=", "/=", "%="</summary>
    public required string Operator { get; init; }

    public required TgmlExpression Value { get; init; }
}

// ── Object / array creation ───────────────────────────────────────────────────

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

/// <summary>
///     Implicit construction: <c>new(args)</c> — type is inferred from the LHS.
///     After checking, <c>Metadata["InferredType"]</c> holds the resolved type name.
/// </summary>
public sealed class TgmlNewImplicitExpr : TgmlExpression
{
    public List<TgmlArgument> Args { get; init; } = new();
}

// ── Intrinsics ────────────────────────────────────────────────────────────────

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

// ── Base access ───────────────────────────────────────────────────────────────

public sealed class TgmlBaseCallExpr : TgmlExpression
{
    public required string MethodName { get; init; }
    public List<TgmlArgument> Args { get; init; } = new();
}

public sealed class TgmlBaseAccessExpr : TgmlExpression
{
    public required string MemberName { get; init; }
}

// ── Lambda ────────────────────────────────────────────────────────────────────

public sealed class TgmlLambdaExpr : TgmlExpression
{
    public List<TgmlParam> Params { get; init; } = new();
    public TgmlExpression? ExprBody { get; init; }
    public TgmlBlock? BlockBody { get; init; }
}

// ── Array initializer ─────────────────────────────────────────────────────────

public sealed class TgmlArrayInitExpr : TgmlExpression
{
    public List<TgmlExpression> Elements { get; init; } = new();
}

// ── Primary ───────────────────────────────────────────────────────────────────

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

public sealed class TgmlNullExpr : TgmlExpression
{
}

public sealed class TgmlFieldKeywordExpr : TgmlExpression
{
}

public sealed class TgmlValueKeywordExpr : TgmlExpression
{
}

// ── Literals ──────────────────────────────────────────────────────────────────

public sealed class TgmlBoolLiteralExpr : TgmlExpression
{
    public required bool Value { get; init; }
}

public sealed class TgmlIntLiteralExpr : TgmlExpression
{
    /// <summary>Raw token text (may be decimal, hex 0x…, or binary 0b…).</summary>
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

    public double ParsedValue =>
        double.Parse(RawValue, CultureInfo.InvariantCulture);
}

public sealed class TgmlStringLiteralExpr : TgmlExpression
{
    /// <summary>Raw token text including surrounding quotes.</summary>
    public required string RawValue { get; init; }

    /// <summary>String content without surrounding quotes.</summary>
    public string Value => RawValue.Length >= 2 ? RawValue[1..^1] : string.Empty;
}
