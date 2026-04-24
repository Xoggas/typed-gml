using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private HashSet<string> CollectReachableTypeNames(TgmlTypeDecl decl, HashSet<string> visited)
    {
        var qualifiedName = decl.QualifiedName ?? decl.Name;
        if (!visited.Add(qualifiedName))
            return visited;

        visited.Add(decl.Name);

        var bases = decl switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            TgmlInterfaceDecl iface => iface.BaseInterfaces,
            _ => null
        };

        if (bases is not null)
        {
            foreach (var baseRef in bases)
            {
                visited.Add(baseRef.Name.Full);
                if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                    CollectReachableTypeNames(baseDecl, visited);
            }
        }

        if (ObjectFacts.TryResolveImplicitObject(_ctx.TypeTable, decl, out var systemObject))
        {
            visited.Add("Object");
            visited.Add(ObjectFacts.SystemObjectQualifiedName);
            CollectReachableTypeNames(systemObject, visited);
        }

        return visited;
    }

    private bool TryResolveTypeDecl(string typeName, out TgmlTypeDecl? decl)
    {
        var (baseTypeName, typeArgs) = DelegateFacts.SplitDescribedType(typeName);
        var typeArgCount = typeArgs.Count;

        while (baseTypeName.EndsWith("[]", StringComparison.Ordinal))
            baseTypeName = baseTypeName[..^2];

        if (BuiltinTypeFacts.TryGetBclTypeName(baseTypeName, out var primitiveBclTypeName))
            baseTypeName = primitiveBclTypeName;

        return _ctx.TypeTable.TryResolve(baseTypeName, typeArgCount, out decl);
    }

    private IReadOnlyDictionary<string, string> BuildGenericTypeBindings(string describedType, TgmlTypeDecl decl)
    {
        var (_, typeArgs) = DelegateFacts.SplitDescribedType(describedType);
        if (typeArgs.Count == 0 || decl.TypeParams.Count == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < Math.Min(typeArgs.Count, decl.TypeParams.Count); i++)
            bindings[decl.TypeParams[i].Name] = typeArgs[i];

        return bindings;
    }

    private TgmlTypeRef SubstituteTypeRef(TgmlTypeRef typeRef, IReadOnlyDictionary<string, string> bindings)
    {
        if (bindings.Count == 0)
            return typeRef;

        if (typeRef.ArrayDepth == 0 &&
            typeRef.TypeArgs.Count == 0 &&
            bindings.TryGetValue(typeRef.Name.Full, out var mappedType))
        {
            return MakeTypeRef(mappedType);
        }

        if (typeRef.TypeArgs.Count == 0)
            return typeRef;

        return new TgmlTypeRef
        {
            Name = typeRef.Name,
            ArrayDepth = typeRef.ArrayDepth,
            TypeArgs = typeRef.TypeArgs.Select(arg => SubstituteTypeRef(arg, bindings)).ToList()
        };
    }

    private TgmlMethodDecl SubstituteMethodDecl(TgmlMethodDecl method, IReadOnlyDictionary<string, string> bindings)
    {
        if (bindings.Count == 0)
            return method;

        var substituted = new TgmlMethodDecl
        {
            Name = method.Name,
            Access = method.Access,
            Line = method.Line,
            Column = method.Column,
            Decorators = method.Decorators,
            ReturnType = SubstituteTypeRef(method.ReturnType, bindings),
            Modifiers = method.Modifiers,
            TypeParams = method.TypeParams,
            Params = method.Params.Select(param => new TgmlParam
            {
                Name = param.Name,
                Decorators = param.Decorators,
                Default = param.Default,
                Type = SubstituteTypeRef(param.Type, bindings)
            }).ToList(),
            OperatorToken = method.OperatorToken,
            Conversion = method.Conversion,
            Body = method.Body
        };

        foreach (var entry in method.Metadata)
            substituted.Metadata[entry.Key] = entry.Value;

        return substituted;
    }

    private static void SetResolvedOperator(TgmlExpression expr, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        expr.Metadata[ResolvedOperatorOwnerMetadata] = owner;
        expr.Metadata[ResolvedOperatorMethodMetadata] = method;
    }

    private static void ClearResolvedOperator(TgmlExpression expr)
    {
        expr.Metadata.Remove(ResolvedOperatorOwnerMetadata);
        expr.Metadata.Remove(ResolvedOperatorMethodMetadata);
    }

    private static void SetResolvedConversion(TgmlExpression expr, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        expr.Metadata[ResolvedConversionOwnerMetadata] = owner;
        expr.Metadata[ResolvedConversionMethodMetadata] = method;
    }

    private static void ClearResolvedConversion(TgmlExpression expr)
    {
        expr.Metadata.Remove(ResolvedConversionOwnerMetadata);
        expr.Metadata.Remove(ResolvedConversionMethodMetadata);
    }

    // -- Utilities -------------------------------------------------------------

    /// <summary>Checks that <paramref name="valueExpr" />'s type is assignable to <paramref name="targetType" />.</summary>
}
