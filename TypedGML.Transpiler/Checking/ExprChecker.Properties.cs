using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private void CheckAssignmentTarget(TgmlExpression target)
    {
        switch (target)
        {
            case TgmlFieldAccessExpr fieldAccess:
                CheckExpr(fieldAccess.Target);
                break;

            case TgmlIndexExpr indexExpr:
                CheckExpr(indexExpr.Target);
                CheckExpr(indexExpr.Index);
                break;

            default:
                break;
        }
    }

    private void CheckPropertyWrite(TgmlExpression target, bool requireRead)
    {
        var prop = ResolvePropertyTarget(target);
        if (prop is not null)
        {
            if (requireRead)
                EnsureReadable(prop, target);

            EnsureWritable(prop, target);
            return;
        }

        var indexer = ResolveIndexerTarget(target);
        if (indexer is null)
            return;

        if (requireRead)
            EnsureReadable(indexer, target);

        EnsureWritable(indexer, target);
    }

    private PropertyAccessHelper.ResolvedProperty? ResolvePropertyOnCurrentType(string name)
    {
        return PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, _owner, name);
    }

    private PropertyAccessHelper.ResolvedProperty? ResolvePropertyTarget(TgmlExpression target)
    {
        return target switch
        {
            TgmlIdExpr id => ResolveBarePropertyTarget(id),
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" }, FieldName: var fieldName }
                => ResolvePropertyOnCurrentType(fieldName),
            TgmlFieldAccessExpr fieldAccess => ResolvePropertyOnResolvedTarget(fieldAccess),
            _ => null
        };
    }

    private PropertyAccessHelper.ResolvedProperty? ResolvePropertyOnResolvedTarget(TgmlFieldAccessExpr target)
    {
        var targetType = InferType(target.Target);
        if (targetType is null) return null;

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        return PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, target.FieldName);
    }

    private PropertyAccessHelper.ResolvedProperty? ResolveIndexerTarget(TgmlExpression target)
    {
        if (target is not TgmlIndexExpr indexExpr)
            return null;

        var targetType = InferType(indexExpr.Target);
        if (targetType is null || TryGetArrayElementType(targetType, out _))
            return null;

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        var indexer = PropertyAccessHelper.FindIndexerInHierarchy(_ctx.TypeTable, targetDecl);
        if (indexer?.Property.IndexParam is null)
            return null;

        ValidateIndexerArgument(indexer, indexExpr.Index, indexExpr);
        return indexer;
    }

    private void EnsureReadable(PropertyAccessHelper.ResolvedProperty resolved, TgmlExpression expr)
    {
        if (resolved.Property.Getter is null)
        {
            Error(expr, $"Property '{resolved.Property.Name}' has no getter.");
            return;
        }

        var access = PropertyAccessHelper.EffectiveGetterAccess(resolved.Property);
        if (!PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, resolved.DeclaringType, access))
            Error(expr, $"Getter of property '{resolved.Property.Name}' is inaccessible due to its protection level.");
    }

    private void EnsureWritable(PropertyAccessHelper.ResolvedProperty resolved, TgmlExpression expr)
    {
        if (resolved.Property.Setter is null)
        {
            Error(expr, $"Property '{resolved.Property.Name}' has no setter.");
            return;
        }

        var access = PropertyAccessHelper.EffectiveSetterAccess(resolved.Property);
        if (!PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, resolved.DeclaringType, access))
            Error(expr, $"Setter of property '{resolved.Property.Name}' is inaccessible due to its protection level.");
    }

    private PropertyAccessHelper.ResolvedProperty? ResolveBarePropertyTarget(TgmlIdExpr id)
    {
        if (id.Name is "self" or "this")
            return null;

        if (Symbols.TryResolve(id.Name, out var symbol) && symbol is not null)
            return null;

        return ResolvePropertyOnCurrentType(id.Name);
    }

    private void ValidateIndexerArgument(
        PropertyAccessHelper.ResolvedProperty indexer,
        TgmlExpression indexExpr,
        TgmlExpression errorSite)
    {
        if (indexer.Property.IndexParam is null)
            return;

        var indexTypeName = DefaultExpressionFacts.DescribeType(indexer.Property.IndexParam.Type);
        if (CanConvertExpression(indexTypeName, indexExpr, allowExplicit: false, apply: true))
            return;

        var actualType = InferType(indexExpr);
        if (actualType is not null)
            Error(errorSite, $"Cannot use index of type '{actualType}' on indexer parameter type '{indexTypeName}'.");
    }

}
