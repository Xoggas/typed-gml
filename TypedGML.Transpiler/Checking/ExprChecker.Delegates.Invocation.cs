using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool TryResolveBareDelegateInvoke(TgmlFuncCallExpr fc, out string? returnType)
    {
        fc.Metadata.Remove(DelegateInvokeTargetMetadata);

        if (!TryResolveDelegateSignatureFromIdentifier(fc.FunctionName, out var signature))
        {
            returnType = null;
            return false;
        }

        if (!TryBindDelegateArguments(signature, fc.Args, out var normalizedArgs, out var error))
        {
            Error(fc, $"Delegate invocation does not match the supplied arguments: {error}");
            returnType = null;
            return true;
        }

        fc.Metadata["NormalizedArgs"] = normalizedArgs;
        fc.Metadata[DelegateInvokeTargetMetadata] = fc.FunctionName;
        fc.Metadata[DelegateInvokeTypeMetadata] = signature.TypeName;
        returnType = signature.ReturnType;
        return true;
    }

    private bool TryResolveDelegateMemberInvoke(TgmlMethodCallExpr mc, TgmlTypeDecl targetDecl, out string? returnType)
    {
        mc.Metadata.Remove(DelegateInvokeTargetMetadata);

        if (!TryResolveDelegateSignatureFromMember(targetDecl, mc.MethodName, out var signature))
        {
            returnType = null;
            return false;
        }

        if (!TryBindDelegateArguments(signature, mc.Args, out var normalizedArgs, out var error))
        {
            Error(mc, $"Delegate invocation does not match the supplied arguments: {error}");
            returnType = null;
            return true;
        }

        mc.Metadata["NormalizedArgs"] = normalizedArgs;
        mc.Metadata[DelegateInvokeTargetMetadata] = mc.MethodName;
        mc.Metadata[DelegateInvokeTypeMetadata] = signature.TypeName;
        returnType = signature.ReturnType;
        return true;
    }

    private bool TryResolveDelegateSignatureFromIdentifier(string name, out DelegateSignature signature)
    {
        if (Symbols.TryResolve(name, out var symbolType) && symbolType is not null)
            return DelegateFacts.TryResolveSignature(_ctx.TypeTable, DefaultExpressionFacts.DescribeType(symbolType), out signature);

        var prop = ResolvePropertyOnCurrentType(name);
        if (prop is not null)
            return DelegateFacts.TryResolveSignature(_ctx.TypeTable, DefaultExpressionFacts.DescribeType(prop.Property.Type), out signature);

        var field = FindFieldInHierarchy(_owner, name);
        if (field is not null)
            return DelegateFacts.TryResolveSignature(_ctx.TypeTable, DefaultExpressionFacts.DescribeType(field.Type), out signature);

        signature = null!;
        return false;
    }

    private bool TryResolveDelegateSignatureFromMember(TgmlTypeDecl targetDecl, string memberName, out DelegateSignature signature)
    {
        var prop = PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, memberName);
        if (prop is not null)
            return DelegateFacts.TryResolveSignature(_ctx.TypeTable, DefaultExpressionFacts.DescribeType(prop.Property.Type), out signature);

        var field = FindFieldInHierarchy(targetDecl, memberName);
        if (field is not null)
            return DelegateFacts.TryResolveSignature(_ctx.TypeTable, DefaultExpressionFacts.DescribeType(field.Type), out signature);

        signature = null!;
        return false;
    }

    private bool TryBindDelegateArguments(
        DelegateSignature signature,
        IReadOnlyList<TgmlArgument> suppliedArgs,
        out List<TgmlExpression> normalizedArgs,
        out string? error)
    {
        normalizedArgs = new List<TgmlExpression>(signature.Parameters.Count);

        if (suppliedArgs.Any(arg => arg.Name is not null))
        {
            error = "Named arguments are not supported for delegate invocation.";
            return false;
        }

        if (suppliedArgs.Count != signature.Parameters.Count)
        {
            error = $"Expected {signature.Parameters.Count} argument(s) but got {suppliedArgs.Count}.";
            return false;
        }

        for (var i = 0; i < signature.Parameters.Count; i++)
        {
            var arg = suppliedArgs[i].Value;
            if (!CanConvertExpression(signature.Parameters[i].TypeName, arg, allowExplicit: false, apply: true))
            {
                var inferred = InferType(arg);
                error = inferred is null
                    ? $"Argument {i + 1} is incompatible with parameter '{signature.Parameters[i].Name}'."
                    : $"Cannot assign argument of type '{inferred}' to parameter '{signature.Parameters[i].Name}' of type '{signature.Parameters[i].TypeName}'.";
                return false;
            }

            normalizedArgs.Add(arg);
        }

        error = null;
        return true;
    }

}
