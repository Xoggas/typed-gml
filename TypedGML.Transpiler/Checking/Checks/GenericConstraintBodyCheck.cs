using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Walks expression bodies and validates that generic type arguments in
///     <c>new</c> expressions satisfy the corresponding type parameter constraints.
///     Complements <see cref="GenericConstraintCheck"/> which only covers declaration-site references.
/// </summary>
public sealed class GenericConstraintBodyCheck : AstBodyWalker
{
    public override string Name => "GenericConstraintBodyCheck";

    protected override void OnExpression(TranspileContext ctx, TgmlFile file,
        TgmlExpression expr, WalkContext wctx)
    {
        if (expr is TgmlNewObjectExpr newObj && newObj.Type.TypeArgs.Count > 0)
            CheckTypeRef(ctx, file, newObj.Type, wctx.OwnerType?.TypeParams ?? []);

        if (expr is TgmlNewArrayExpr newArr && newArr.ElementType?.TypeArgs.Count > 0)
            CheckTypeRef(ctx, file, newArr.ElementType, wctx.OwnerType?.TypeParams ?? []);
    }

    private static void CheckTypeRef(TranspileContext ctx, TgmlFile file,
        TgmlTypeRef typeRef, IEnumerable<TgmlTypeParam> visibleTypeParams)
    {
        var visible = visibleTypeParams as List<TgmlTypeParam> ?? visibleTypeParams.ToList();

        foreach (var arg in typeRef.TypeArgs)
            CheckTypeRef(ctx, file, arg, visible);

        if (typeRef.TypeArgs.Count == 0)
            return;

        if (!ctx.TypeTable.TryResolve(typeRef.Name.Full, out var resolvedDecl) || resolvedDecl is null)
            return;

        var typeParams = resolvedDecl.TypeParams;

        for (var i = 0; i < Math.Min(typeRef.TypeArgs.Count, typeParams.Count); i++)
        {
            var constraint = typeParams[i].Constraint;
            if (constraint is null)
                continue;

            var argRef = typeRef.TypeArgs[i];

            if (visible.Any(tp => tp.Name == argRef.Name.Full))
                continue;

            if (!ctx.TypeTable.TryResolve(argRef.Name.Full, out var argDecl) || argDecl is null)
                continue;

            if (!SatisfiesConstraint(argDecl, constraint, ctx))
            {
                ctx.AddError(
                    $"Type argument '{argRef.Name.Full}' does not satisfy the constraint " +
                    $"'{constraint.Name.Full}' on type parameter '{typeParams[i].Name}' " +
                    $"of '{resolvedDecl.QualifiedName ?? resolvedDecl.Name}'.",
                    file.FileName);
            }
        }
    }

    private static bool SatisfiesConstraint(TgmlTypeDecl argDecl, TgmlTypeRef constraint, TranspileContext ctx)
    {
        var constraintName = constraint.Name.Full;
        var allBases = CollectAllBaseNames(argDecl, ctx, new HashSet<string>(StringComparer.Ordinal));
        return allBases.Contains(constraintName)
               || argDecl.QualifiedName == constraintName
               || argDecl.Name == constraintName;
    }

    private static HashSet<string> CollectAllBaseNames(TgmlTypeDecl decl, TranspileContext ctx, HashSet<string> visited)
    {
        var qn = decl.QualifiedName ?? decl.Name;
        if (!visited.Add(qn))
            return visited;

        var bases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            TgmlInterfaceDecl i => i.BaseInterfaces,
            _ => null
        };

        if (bases is null)
            return visited;

        foreach (var b in bases)
        {
            visited.Add(b.Name.Full);
            if (ctx.TypeTable.TryResolve(b.Name.Full, out var baseDecl) && baseDecl is not null)
                CollectAllBaseNames(baseDecl, ctx, visited);
        }

        return visited;
    }
}

