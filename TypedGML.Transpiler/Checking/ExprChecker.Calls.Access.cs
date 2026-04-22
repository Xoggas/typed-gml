using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private string? VisitIndex(TgmlIndexExpr ix)
    {
        CheckExpr(ix.Target);
        CheckExpr(ix.Index);

        var targetType = InferType(ix.Target);
        if (targetType is null)
            return null;

        if (TryGetArrayElementType(targetType, out var elementType))
            return elementType;

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        var indexer = PropertyAccessHelper.FindIndexerInHierarchy(_ctx.TypeTable, targetDecl);
        if (indexer?.Property.IndexParam is null)
            return null;

        EnsureReadable(indexer, ix);
        ValidateIndexerArgument(indexer, ix.Index, ix);
        return DefaultExpressionFacts.DescribeType(indexer.Property.Type);
    }

    private string? VisitInvoke(TgmlInvokeExpr invoke)
    {
        var targetType = InferType(invoke.Target);
        foreach (var arg in invoke.Args) CheckExpr(arg.Value);

        if (targetType is null || !DelegateFacts.TryResolveSignature(_ctx.TypeTable, targetType, out var signature))
            return null;

        if (!TryBindDelegateArguments(signature, invoke.Args, out var normalizedArgs, out var error))
        {
            Error(invoke, $"Delegate invocation does not match the supplied arguments: {error}");
            return null;
        }

        invoke.Metadata["NormalizedArgs"] = normalizedArgs;
        invoke.Metadata[DelegateInvokeTypeMetadata] = signature.TypeName;
        return signature.ReturnType;
    }

    private string? VisitFieldAccess(TgmlFieldAccessExpr fa)
    {
        if (TryResolveStaticAssetMemberAccess(fa, out var assetName, out var assetType))
        {
            fa.Metadata[AssetReferenceNameMetadata] = assetName;
            return assetType;
        }

        var targetType = InferType(fa.Target);
        if (targetType is null) return null;

        var resolvedTargetType = MapPrimitiveType(targetType);
        if (resolvedTargetType is null ||
            !_ctx.TypeTable.TryResolve(resolvedTargetType, out var targetDecl) || targetDecl is null)
            return null;

        var prop = PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, fa.FieldName);
        if (prop is not null)
        {
            EnsureReadable(prop, fa);
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);
        }

        var (field, fieldDeclaringType) = FindFieldWithDecl(targetDecl, fa.FieldName);
        if (field is null)
        {
            Error(fa, $"Type '{targetType}' does not contain a member '{fa.FieldName}'.");
            return null;
        }

        if (!PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, fieldDeclaringType!, field.Access))
            Error(fa, $"Field '{fa.FieldName}' is inaccessible due to its protection level.");

        return DefaultExpressionFacts.DescribeType(field.Type);
    }

    private string? VisitLambda(TgmlLambdaExpr lam)
    {
        if (lam.Metadata.TryGetValue(ContextualDelegateTypeMetadata, out var contextualType) &&
            contextualType is string delegateType)
        {
            return delegateType;
        }

        return null;
    }

}
