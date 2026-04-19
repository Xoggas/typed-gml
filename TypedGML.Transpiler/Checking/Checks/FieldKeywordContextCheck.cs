using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Restricts the <c>field</c> contextual keyword to supported accessor bodies.
/// </summary>
public sealed class FieldKeywordContextCheck : AstBodyWalker
{
    public override string Name => "FieldKeywordContextCheck";

    protected override void OnExpression(TranspileContext ctx, TgmlFile file, TgmlExpression expr, WalkContext wctx)
    {
        if (expr is not TgmlFieldKeywordExpr)
            return;

        if (wctx.Member is not TgmlPropertyDecl property)
        {
            ctx.AddError(
                "The 'field' keyword can only be used inside a property accessor.",
                file.FileName,
                expr.Line,
                expr.Column);
            return;
        }

        if (property.IsIndexer)
        {
            ctx.AddError(
                "The 'field' keyword is not supported inside indexer accessors.",
                file.FileName,
                expr.Line,
                expr.Column);
        }
    }
}
