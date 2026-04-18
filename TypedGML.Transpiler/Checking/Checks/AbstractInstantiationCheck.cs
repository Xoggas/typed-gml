using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Reports an error when <c>new T(...)</c> is used and <c>T</c> is an abstract class.
/// </summary>
public sealed class AbstractInstantiationCheck : AstBodyWalker
{
    public override string Name => "AbstractInstantiationCheck";

    protected override void OnExpression(TranspileContext ctx, TgmlFile file,
        TgmlExpression expr, WalkContext wctx)
    {
        if (expr is TgmlNewObjectExpr n
            && ctx.TypeTable.TryResolve(n.Type.Name.Full, out var decl)
            && decl is TgmlClassDecl { IsAbstract: true })
        {
            ctx.AddError(
                $"Cannot instantiate abstract class '{decl.QualifiedName ?? decl.Name}'.",
                file.FileName, n.Line, n.Column);
        }
    }
}