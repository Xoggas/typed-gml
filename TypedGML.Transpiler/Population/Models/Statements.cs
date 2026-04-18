namespace TypedGML.Transpiler.Population.Models;

// ── All statement node types ─────────────────────────────────────────────────

/// <summary>Base class for all TypedGML statement nodes.</summary>
public abstract class TgmlStatement
{
    public int Line { get; init; }
    public int Column { get; init; }
}

// ── Block ─────────────────────────────────────────────────────────────────────

public sealed class TgmlBlock : TgmlStatement
{
    public List<TgmlStatement> Statements { get; init; } = new();
}

// ── Local variable declaration ────────────────────────────────────────────────

public sealed class TgmlLocalVarDecl : TgmlStatement
{
    public required TgmlTypeRef Type { get; init; }
    public required string Name { get; init; }
    public TgmlExpression? Initializer { get; init; }
}

// ── Expression statement ──────────────────────────────────────────────────────

public sealed class TgmlExpressionStmt : TgmlStatement
{
    public required TgmlExpression Expression { get; init; }
}

// ── If / else-if / else ───────────────────────────────────────────────────────

public sealed class TgmlIfBranch
{
    public required TgmlExpression Condition { get; init; }
    public required TgmlBlock Body { get; init; }
}

public sealed class TgmlIfStmt : TgmlStatement
{
    /// <summary>The primary if + all else-if branches.</summary>
    public required List<TgmlIfBranch> Branches { get; init; }

    public TgmlBlock? ElseBlock { get; init; }
}

// ── While ─────────────────────────────────────────────────────────────────────

public sealed class TgmlWhileStmt : TgmlStatement
{
    public required TgmlExpression Condition { get; init; }
    public required TgmlBlock Body { get; init; }
}

// ── For ───────────────────────────────────────────────────────────────────────

public abstract class TgmlForInit
{
}

public sealed class TgmlForInitDecl : TgmlForInit
{
    public required TgmlTypeRef Type { get; init; }
    public required string Name { get; init; }
    public TgmlExpression? Initializer { get; init; }
}

public sealed class TgmlForInitExpr : TgmlForInit
{
    public required TgmlExpression Expression { get; init; }
}

public sealed class TgmlForStmt : TgmlStatement
{
    public TgmlForInit? Init { get; init; }
    public TgmlExpression? Condition { get; init; }
    public List<TgmlExpression> Updates { get; init; } = new();
    public required TgmlBlock Body { get; init; }
}

// ── Repeat ────────────────────────────────────────────────────────────────────

public sealed class TgmlRepeatStmt : TgmlStatement
{
    public required TgmlExpression Count { get; init; }
    public required TgmlBlock Body { get; init; }
}

// ── Switch ────────────────────────────────────────────────────────────────────

public sealed class TgmlSwitchSection
{
    /// <summary>null means this is the 'default' section.</summary>
    public TgmlExpression? CaseValue { get; init; }

    public bool IsDefault => CaseValue is null;
    public List<TgmlStatement> Statements { get; init; } = new();
}

public sealed class TgmlSwitchStmt : TgmlStatement
{
    public required TgmlExpression Value { get; init; }
    public List<TgmlSwitchSection> Sections { get; init; } = new();
}

// ── With ──────────────────────────────────────────────────────────────────────

public sealed class TgmlWithStmt : TgmlStatement
{
    public required TgmlTypeRef IterType { get; init; }
    public required string VarName { get; init; }
    public required TgmlBlock Body { get; init; }
}

// ── Return ────────────────────────────────────────────────────────────────────

public sealed class TgmlReturnStmt : TgmlStatement
{
    public TgmlExpression? Value { get; init; }
}

// ── Break / Continue ──────────────────────────────────────────────────────────

public sealed class TgmlBreakStmt : TgmlStatement
{
}

public sealed class TgmlContinueStmt : TgmlStatement
{
}

// ── Try / Catch / Finally ─────────────────────────────────────────────────────

public sealed class TgmlCatchClause
{
    public required TgmlTypeRef ExceptionType { get; init; }
    public required string VarName { get; init; }
    public required TgmlBlock Body { get; init; }
}

public sealed class TgmlTryStmt : TgmlStatement
{
    public required TgmlBlock TryBlock { get; init; }
    public List<TgmlCatchClause> CatchClauses { get; init; } = new();
    public TgmlBlock? FinallyBlock { get; init; }
}

// ── Raw GML ───────────────────────────────────────────────────────────────────

public sealed class TgmlRawStmt : TgmlStatement
{
    /// <summary>Full raw line text including the leading '#'.</summary>
    public required string RawText { get; init; }
}