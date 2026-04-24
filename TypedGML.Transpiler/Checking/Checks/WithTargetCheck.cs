using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates that TypedGML <c>with (Type alias)</c> statements target GameMaker object types.
/// </summary>
public sealed class WithTargetCheck : AstBodyWalker
{
    public override string Name => "WithTargetCheck";

    protected override bool OnStatement(
        TranspileContext ctx,
        TgmlFile file,
        TgmlStatement stmt,
        WalkContext wctx)
    {
        if (stmt is not TgmlWithStmt withStmt)
            return true;

        var typeName = withStmt.IterType.Name.Full;
        if (!ctx.TypeTable.TryResolve(typeName, withStmt.IterType.TypeArgs.Count, out var targetDecl) ||
            targetDecl is null)
        {
            return true;
        }

        if (targetDecl is not TgmlClassDecl { IsGameObject: true })
        {
            ctx.AddError(
                $"'with' target type '{typeName}' must be a GameMaker object class decorated with @Object.",
                file.FileName,
                withStmt.Line,
                withStmt.Column);
        }

        return true;
    }
}
