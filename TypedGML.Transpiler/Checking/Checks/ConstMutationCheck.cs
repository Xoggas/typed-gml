using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Prevents mutation of <c>const</c> fields anywhere, and of
///     <c>readonly</c> fields outside of constructors (including cross-type access).
/// </summary>
public sealed class ConstMutationCheck : AstBodyWalker
{
    public override string Name => "ConstMutationCheck";

    protected override void OnExpression(TranspileContext ctx, TgmlFile file,
        TgmlExpression expr, WalkContext wctx)
    {
        if (expr is not TgmlAssignExpr assign) return;

        // Case 1: own field (bare name or self./this. prefix)
        var ownFieldName = ExtractOwnFieldName(assign.Target);
        if (ownFieldName is not null)
        {
            var field = FindField(wctx.OwnerType, ownFieldName);
            if (field is null) return;
            ValidateFieldMutation(ctx, file, field, ownFieldName, assign, wctx.InConstructor);
            return;
        }

        // Case 2: cross-type field access  (someExpr.FieldName = ...)
        if (assign.Target is TgmlFieldAccessExpr crossAccess &&
            crossAccess.Target is not TgmlIdExpr { Name: "self" or "this" })
        {
            var inferredTypeName = crossAccess.Target.Metadata.TryGetValue("InferredType", out var inf)
                ? inf as string : null;
            if (inferredTypeName is null) return;
            if (!ctx.TypeTable.TryResolve(inferredTypeName, out var targetDecl) || targetDecl is null) return;

            var field = FindField(targetDecl, crossAccess.FieldName);
            if (field is null) return;

            // Cross-type: never considered inside the declaring type's constructor
            ValidateFieldMutation(ctx, file, field, crossAccess.FieldName, assign, inConstructor: false);
        }
    }

    private static void ValidateFieldMutation(TranspileContext ctx, TgmlFile file,
        TgmlFieldDecl field, string fieldName, TgmlAssignExpr assign, bool inConstructor)
    {
        if (field.IsConst)
        {
            ctx.AddError($"Cannot assign to const field '{fieldName}'.",
                file.FileName, assign.Line, assign.Column);
        }
        else if (field.IsReadonly && !inConstructor)
        {
            ctx.AddError($"Cannot assign to readonly field '{fieldName}' outside of a constructor.",
                file.FileName, assign.Line, assign.Column);
        }
    }

    private static string? ExtractOwnFieldName(TgmlExpression target) =>
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

