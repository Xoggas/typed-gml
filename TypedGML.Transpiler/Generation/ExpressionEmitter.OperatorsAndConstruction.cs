using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private string EmitUnary(TgmlUnaryExpr e)
    {
        if (TryGetResolvedNativeOperator(e, out var nativeOperator))
            return $"{NativeOperatorFacts.ToGmlToken(nativeOperator)}{Emit(e.Operand)}";

        if (TryGetResolvedOperatorHelper(e, out var operatorHelper))
            return $"{operatorHelper}({Emit(e.Operand)})";

        return $"{(e.Operator == "not" ? "!" : e.Operator)}{Emit(e.Operand)}";
    }

    private string EmitBinary(TgmlBinaryExpr e)
    {
        if (TryGetResolvedNativeOperator(e, out var nativeOperator))
            return $"{Emit(e.Left)} {NativeOperatorFacts.ToGmlToken(nativeOperator)} {Emit(e.Right)}";

        if (TryGetResolvedOperatorHelper(e, out var operatorHelper))
            return $"{operatorHelper}({Emit(e.Left)}, {Emit(e.Right)})";

        var op = e.Operator switch
        {
            "and" => "&&",
            "or" => "||",
            _ => e.Operator
        };
        return $"{Emit(e.Left)} {op} {Emit(e.Right)}";
    }

    private string EmitCast(TgmlCastExpr e)
        => TryGetResolvedConversionHelper(e.Operand, out var conversionHelper)
            ? $"{conversionHelper}({EmitCore(e.Operand)})"
            : Emit(e.Operand);

    private string EmitInvoke(TgmlInvokeExpr e) => EmitDelegateInvoke(e.Target, e, GetNormalizedArgs(e, e.Args));

    private string EmitIs(TgmlIsExpr e)
    {
        var obj = Emit(e.Operand);
        return $"(is_struct({obj}) && variable_struct_exists({obj}.__types, __TYPE_{GmlTypeName(e.CheckType)}))";
    }

    private string EmitAs(TgmlAsExpr e)
    {
        var obj = Emit(e.Operand);
        return $"((is_struct({obj}) && variable_struct_exists({obj}.__types, __TYPE_{GmlTypeName(e.TargetType)})) ? {obj} : undefined)";
    }

    private string EmitNewObject(TgmlNewObjectExpr e)
    {
        var typeName = e.Type.Name.Full;
        _ctx.TypeTable.TryResolve(typeName, out var decl);

        if (decl is TgmlClassDecl cls && cls.IsGameObject)
        {
            var plan = GameObjectConstructionHelper.Build(e, cls, _ctx, this);
            return GameObjectConstructionHelper.BuildConstructorCall(plan);
        }

        var gmlName = decl?.QualifiedName?.Replace(".", "_") ?? e.Type.GmlBaseName;
        var typeArgIds = e.Type.TypeArgs.Select(EmitTypeRefAsRuntimeId).ToList();
        var userArgs = GetNormalizedArgs(e, e.Args).Select(Emit).ToList();
        return $"new {gmlName}({string.Join(", ", typeArgIds.Concat(userArgs))})";
    }

    private string EmitNewImplicit(TgmlNewImplicitExpr e)
    {
        var typeName = e.Metadata.TryGetValue("InferredType", out var inferredType) ? inferredType as string : null;
        if (typeName is null)
            return "undefined";

        _ctx.TypeTable.TryResolve(typeName, out var decl);
        var gmlName = decl?.QualifiedName?.Replace(".", "_") ?? typeName;
        return $"new {gmlName}({string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit))})";
    }

    private string EmitDictionaryInit(TgmlDictionaryInitExpr e)
    {
        if (!e.Metadata.TryGetValue(DictionaryLiteralConcreteTypeMetadata, out var concreteTypeObj) ||
            !e.Metadata.TryGetValue(DictionaryLiteralKeyTypeMetadata, out var keyTypeObj) ||
            !e.Metadata.TryGetValue(DictionaryLiteralValueTypeMetadata, out var valueTypeObj) ||
            concreteTypeObj is not string concreteType ||
            keyTypeObj is not string keyType ||
            valueTypeObj is not string valueType)
        {
            return "undefined";
        }

        var gmlName = concreteType[..concreteType.IndexOf('<')].Replace(".", "_");
        var runtimeArgs = $"{EmitRuntimeTypeId(keyType)}, {EmitRuntimeTypeId(valueType)}";
        if (e.Entries.Count == 0)
            return $"new {gmlName}({runtimeArgs})";

        var keys = $"[{string.Join(", ", e.Entries.Select(entry => Emit(entry.Key)))}]";
        var values = $"[{string.Join(", ", e.Entries.Select(entry => Emit(entry.Value)))}]";
        return $"new {gmlName}({runtimeArgs}, {keys}, {values})";
    }

    private string EmitTypeRefAsRuntimeId(TgmlTypeRef typeRef)
    {
        var name = typeRef.Name.Full;
        if (BuiltinTypeFacts.CanonicalPrimitiveName(name) is { } canonical)
            return $"__TYPE_{canonical}";
        if (_ctx.TypeTable.TryResolve(name, out var decl) && decl?.QualifiedName is { } qn)
            return $"__TYPE_{qn.Replace(".", "_")}";

        return $"__TYPE_{name}";
    }

    private string EmitRuntimeTypeId(string describedType)
    {
        var (baseName, _) = DelegateFacts.SplitDescribedType(describedType);
        while (baseName.EndsWith("[]", StringComparison.Ordinal))
            baseName = baseName[..^2];
        if (BuiltinTypeFacts.CanonicalPrimitiveName(baseName) is { } canonical)
            return $"__TYPE_{canonical}";

        if (_ctx.TypeTable.TryResolve(baseName, out var decl) && decl?.QualifiedName is { } qn)
            return $"__TYPE_{qn.Replace(".", "_")}";

        return $"__TYPE_{baseName.Replace(".", "_")}";
    }

    private string EmitBaseCall(TgmlBaseCallExpr e)
    {
        var args = string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit));
        if (_ctx.CurrentType is TgmlClassDecl cls && cls.IsGameObject)
            return $"undefined /* base.{e.MethodName} is inlined for GameObjects */";

        if (_ctx.CurrentType is TgmlClassDecl scriptCls)
        {
            var parentTypeName = scriptCls.BaseTypes.FirstOrDefault()?.Name.Full;
            if (parentTypeName is not null)
            {
                _ctx.TypeTable.TryResolve(parentTypeName, out var parentDecl);
                if (parentDecl is TgmlClassDecl parentCls)
                {
                    var parentGml = parentCls.QualifiedName?.Replace(".", "_") ?? parentTypeName;
                    return $"{parentGml}_{e.MethodName}(self, {args})".TrimEnd(',', ' ').Replace("(self, )", "(self)");
                }
            }
        }

        return $"/* base.{e.MethodName}({args}) */";
    }

    private string EmitBaseAccess(TgmlBaseAccessExpr e)
        => _ctx.CurrentType is TgmlClassDecl { IsGameObject: true } ? e.MemberName : $"/* base.{e.MemberName} */";

    private string EmitLambda(TgmlLambdaExpr e)
    {
        var paramStr = string.Join(", ", e.Params.Select(p => p.Name));
        if (e.ExprBody is not null)
            return $"function({paramStr}) {{ return {Emit(e.ExprBody)}; }}";
        if (e.BlockBody is not null)
        {
            var w = new GmlWriter();
            new StatementEmitter(_ctx).Emit(e.BlockBody, w);
            return $"function({paramStr})\n{{\n{w}}}";
        }

        return $"function({paramStr}) {{}}";
    }

    private IReadOnlyList<TgmlExpression> GetNormalizedArgs(TgmlExpression owner, IReadOnlyList<TgmlArgument> rawArgs)
    {
        if (owner.Metadata.TryGetValue("NormalizedArgs", out var normalizedArgs) &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return rawArgs.Select(a => a.Value).ToList();
    }

    private static bool TryGetResolvedOperatorHelper(TgmlExpression expr, out string helperName)
    {
        if (expr.Metadata.TryGetValue(ResolvedOperatorOwnerMetadata, out var ownerValue) &&
            expr.Metadata.TryGetValue(ResolvedOperatorMethodMetadata, out var methodValue) &&
            ownerValue is TgmlTypeDecl owner &&
            methodValue is TgmlMethodDecl method)
        {
            helperName = OperatorFacts.GetHelperName(owner, method);
            return true;
        }

        helperName = string.Empty;
        return false;
    }

    private static bool TryGetResolvedNativeOperator(TgmlExpression expr, out string nativeOperator)
    {
        nativeOperator = string.Empty;

        return expr.Metadata.TryGetValue(ResolvedOperatorMethodMetadata, out var methodValue) &&
               methodValue is TgmlMethodDecl method &&
               NativeOperatorFacts.TryGetOperatorToken(method, out nativeOperator);
    }

    private static bool TryGetResolvedConversionHelper(TgmlExpression expr, out string helperName)
    {
        if (expr.Metadata.TryGetValue(ResolvedConversionOwnerMetadata, out var ownerValue) &&
            expr.Metadata.TryGetValue(ResolvedConversionMethodMetadata, out var methodValue) &&
            ownerValue is TgmlTypeDecl owner &&
            methodValue is TgmlMethodDecl method)
        {
            helperName = OperatorFacts.GetHelperName(owner, method);
            return true;
        }

        helperName = string.Empty;
        return false;
    }
}
