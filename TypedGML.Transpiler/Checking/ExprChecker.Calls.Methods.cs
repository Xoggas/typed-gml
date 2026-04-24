using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private string? VisitMethodCall(TgmlMethodCallExpr mc)
    {
        var targetType = InferType(mc.Target);
        foreach (var a in mc.Args) CheckExpr(a.Value);

        // Map primitive types to their BCL wrapper class for member lookup
        var resolvedTargetType = MapPrimitiveType(targetType);

        if (resolvedTargetType is null ||
            !TryResolveTypeDecl(resolvedTargetType, out var targetDecl) ||
            targetDecl is null)
        {
                // Check if target is a bare type name (static member access)
                if (mc.Target is TgmlIdExpr { Name: var typeName } &&
                    _ctx.TypeTable.TryResolve(typeName, out var typeDecl) && typeDecl is not null)
                {
                    var typeCandidates = FindMethodsInHierarchy(typeDecl, mc.MethodName);
                    if (typeCandidates.Count == 0)
                    {
                        Error(mc, $"Type '{typeName}' does not define a method '{mc.MethodName}'.");
                        return null;
                    }
                    if (typeCandidates.Any(m => !m.Modifiers.IsStatic))
                    {
                        Error(mc, $"Cannot call instance method '{mc.MethodName}' on type '{typeName}'. Did you mean to call it on an instance?");
                        return null;
                    }

                    // Bind arguments and apply conversions for static method calls
                    if (!CallArgumentBinder.TryResolveOverload(
                            typeCandidates,
                            m => m.Params,
                            mc.Args,
                            InferType,
                            CanAssignImplicitly,
                            out var resolvedStaticMethod,
                            out var staticBound,
                            out var staticError))
                    {
                        Error(mc, $"No overload of '{mc.MethodName}' in '{typeName}' matches the supplied arguments: {staticError}");
                        return null;
                    }

                    mc.Metadata["NormalizedArgs"] = staticBound!.Arguments.ToList();
                    ApplyBoundArgumentConversions(staticBound.Parameters, staticBound.Arguments);
                    return DefaultExpressionFacts.DescribeType(resolvedStaticMethod!.ReturnType);
                }
                else if (mc.Args.Any(a => a.Name is not null))
                    Error(mc, $"Named arguments cannot be used because method '{mc.MethodName}' could not be resolved.");
                return null;
        }

        var bindings = BuildGenericTypeBindings(resolvedTargetType, targetDecl);
        var candidates = FindMethodsInHierarchy(targetDecl, mc.MethodName)
            .Select(method => SubstituteMethodDecl(method, bindings))
            .ToList();
        if (candidates.Count == 0)
        {
            if (TryResolveDelegateMemberInvoke(mc, targetDecl, out var delegateReturnType))
                return delegateReturnType;

            Error(mc, $"Type '{resolvedTargetType}' does not define a method '{mc.MethodName}'.");
            return null;
        }

        if (!CallArgumentBinder.TryResolveOverload(
                candidates,
                m => m.Params,
                mc.Args,
                InferType,
                CanAssignImplicitly,
                out var resolvedMethod,
                out var bound,
                out var error))
        {
            Error(mc, $"No overload of method '{mc.MethodName}' matches the supplied arguments: {error}");
            return null;
        }

        // Propagate NativeInstanceCallName so the emitter knows to pass the instance as first arg.
        // Decorator handlers run in generation, so checking must also read the decorator directly.
        if (TryGetNativeInstanceCallName(resolvedMethod!, out var nativeInstanceCallName))
            mc.Metadata["NativeInstanceCallName"] = nativeInstanceCallName;

        mc.Metadata["ResolvedMethod"] = resolvedMethod!;
        mc.Metadata["ResolvedMethodOwner"] = targetDecl;
        mc.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        return DefaultExpressionFacts.DescribeType(resolvedMethod!.ReturnType);
    }

    private string? VisitFuncCall(TgmlFuncCallExpr fc)
    {
        foreach (var a in fc.Args) CheckExpr(a.Value);

        if (TryResolveBareDelegateInvoke(fc, out var delegateReturnType))
            return delegateReturnType;

        var candidates = FindMethodsInHierarchy(_owner, fc.FunctionName);
        if (candidates.Count == 0)
        {
            if (fc.Args.Any(a => a.Name is not null))
                Error(fc, $"Named arguments cannot be used because function '{fc.FunctionName}' could not be resolved.");
            return null;
        }

        if (!CallArgumentBinder.TryResolveOverload(
                candidates,
                m => m.Params,
                fc.Args,
                InferType,
                CanAssignImplicitly,
                out var resolvedMethod,
                out var bound,
                out var error))
        {
            Error(fc, $"No overload of method '{fc.FunctionName}' matches the supplied arguments: {error}");
            return null;
        }

        fc.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);

        if (_isStaticContext && !resolvedMethod!.Modifiers.IsStatic)
            Error(fc, $"Cannot call instance method '{fc.FunctionName}' from a static context.");

        return DefaultExpressionFacts.DescribeType(resolvedMethod!.ReturnType);
    }

    private static bool TryGetNativeInstanceCallName(TgmlMethodDecl method, out string nativeInstanceCallName)
    {
        if (method.Metadata.TryGetValue("NativeInstanceCallName", out var metadataValue) &&
            metadataValue is string metadataName &&
            !string.IsNullOrWhiteSpace(metadataName))
        {
            nativeInstanceCallName = metadataName;
            return true;
        }

        var decoratorName = method.GetDecorator("NativeInstanceCall")?.GetFirstStringArg();
        if (!string.IsNullOrWhiteSpace(decoratorName))
        {
            nativeInstanceCallName = decoratorName;
            return true;
        }

        nativeInstanceCallName = string.Empty;
        return false;
    }

}
