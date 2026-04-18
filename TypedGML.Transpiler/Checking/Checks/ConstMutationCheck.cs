using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Prevents mutation of <c>const</c> fields anywhere, and of
///     <c>readonly</c> fields outside of constructors.
/// </summary>
public sealed class ConstMutationCheck : AstBodyWalker
{
    public override string Name => "ConstMutationCheck";

    protected override void OnExpression(TranspileContext ctx, TgmlFile file,
        TgmlExpression expr, WalkContext wctx)
    {
        if (expr is not TgmlAssignExpr assign) return;

        var fieldName = ExtractFieldName(assign.Target);
        if (fieldName is null) return;

        var field = FindField(wctx.OwnerType, fieldName);
        if (field is null) return;

        if (field.IsConst)
        {
            ctx.AddError($"Cannot assign to const field '{fieldName}'.",
                file.FileName, assign.Line, assign.Column);
        }
        else if (field.IsReadonly && !wctx.InConstructor)
        {
            ctx.AddError($"Cannot assign to readonly field '{fieldName}' outside of a constructor.",
                file.FileName, assign.Line, assign.Column);
        }
    }

    private static string? ExtractFieldName(TgmlExpression target) =>
        target switch
        {
            TgmlIdExpr id => id.Name,
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa => fa.FieldName,
            _ => null
        };

    private static TgmlFieldDecl? FindField(TgmlTypeDecl owner, string name) =>
        owner switch
        {
            TgmlClassDecl cls => cls.Fields.FirstOrDefault(f => f.Name == name),
            TgmlStructDecl str => str.Fields.FirstOrDefault(f => f.Name == name),
            _ => null
        };
}

