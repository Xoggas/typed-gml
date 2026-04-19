using TypedGML.Transpiler.Population.Models;

using TypedGML.Transpiler.Checking;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Converts TypedGML expression nodes to GML source strings.
/// </summary>
public sealed class ExpressionEmitter
{
    private const string ResolvedOperatorOwnerMetadata = "ResolvedOperatorOwner";
    private const string ResolvedOperatorMethodMetadata = "ResolvedOperatorMethod";
    private const string ResolvedConversionOwnerMetadata = "ResolvedConversionOwner";
    private const string ResolvedConversionMethodMetadata = "ResolvedConversionMethod";
    private const string DelegateInvokeTargetMetadata = "DelegateInvokeTarget";
    private const string DelegateInvokeTypeMetadata = "DelegateInvokeType";
    private const string ContextualDelegateTypeMetadata = "ContextualDelegateType";
    private const string DelegateMethodGroupMetadata = "DelegateMethodGroup";
    private const string AssetReferenceNameMetadata = AssetFacts.AssetReferenceNameMetadata;

    private readonly GenerationContext _ctx;

    public ExpressionEmitter(GenerationContext ctx)
    {
        _ctx = ctx;
    }

    public string Emit(TgmlExpression expr)
    {
        if (TryEmitDelegateConstruction(expr, out var delegateConstruction))
            return delegateConstruction;

        if (expr is not TgmlCastExpr && TryGetResolvedConversionHelper(expr, out var conversionHelper))
            return $"{conversionHelper}({EmitCore(expr)})";

        return EmitCore(expr);
    }

    private string EmitCore(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlMethodCallExpr e => EmitMethodCall(e),
            TgmlFieldAccessExpr e => EmitFieldAccess(e),
            TgmlIndexExpr e => EmitIndex(e),
            TgmlInvokeExpr e => EmitInvoke(e),
            TgmlUnaryExpr e => EmitUnary(e),
            TgmlCastExpr e => EmitCast(e),
            TgmlBinaryExpr e => EmitBinary(e),
            TgmlIsExpr e => EmitIs(e),
            TgmlAsExpr e => EmitAs(e),
            TgmlTernaryExpr e => $"{Emit(e.Condition)} ? {Emit(e.ThenExpr)} : {Emit(e.ElseExpr)}",
            TgmlAssignExpr e => EmitAssign(e),
            TgmlNewObjectExpr e => EmitNewObject(e),
            TgmlNewArrayExpr e => $"array_create({Emit(e.Size)})",
            TgmlTypeofExpr e => $"__TYPE_{GmlTypeName(e.Type)}",
            TgmlNameofExpr e => $"\"{Emit(e.Operand)}\"",
            TgmlDefaultExpr e => EmitDefault(e),
            TgmlBaseCallExpr e => EmitBaseCall(e),
            TgmlBaseAccessExpr e => EmitBaseAccess(e),
            TgmlLambdaExpr e => EmitLambda(e),
            TgmlArrayInitExpr e => $"[{string.Join(", ", e.Elements.Select(Emit))}]",
            TgmlFuncCallExpr e => EmitFuncCall(e),
            TgmlParenExpr e => $"({Emit(e.Inner)})",
            TgmlFieldKeywordExpr _ => EmitFieldKeyword(),
            TgmlValueKeywordExpr _ => "value",
            TgmlIdExpr e => EmitIdRead(e),
            TgmlNullExpr _ => "undefined",
            TgmlBoolLiteralExpr e => e.Value ? "true" : "false",
            TgmlIntLiteralExpr e => e.ParsedValue.ToString(),
            TgmlRealLiteralExpr e => e.RawValue,
            TgmlStringLiteralExpr e => e.RawValue,
            _ => $"/* unsupported expr: {expr.GetType().Name} */"
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GmlTypeName(TgmlTypeRef typeRef)
    {
        // Try to resolve via TypeTable to get qualified GML name
        if (_ctx.TypeTable.TryResolve(typeRef.Name.Full, out var decl) && decl?.QualifiedName is { } qn)
        {
            return qn.Replace(".", "_");
        }

        return typeRef.GmlBaseName;
    }

    private string EmitFieldKeyword()
    {
        if (_ctx.CurrentPropertyName is { } currentPropertyName &&
            _ctx.GetNativePropertyName(currentPropertyName) is { } nativePropertyName)
        {
            return nativePropertyName;
        }

        if (_ctx.CurrentPropertyName is { } pn2)
        {
            return $"__backing_{pn2}";
        }

        return "__backing_field";
    }

    private string EmitDefault(TgmlDefaultExpr expr)
    {
        var typeName = DefaultExpressionFacts.GetEffectiveTypeName(expr);
        if (typeName is null)
            return "undefined";

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
            return "undefined";

        if (typeName == "bool")
            return "false";

        if (BuiltinTypeFacts.IsNumeric(typeName))
            return "0";

        if (TryResolveDefaultType(typeName, out var decl) && decl is TgmlEnumDecl)
            return "0";

        if (decl is TgmlStructDecl structDecl)
            return EmitDefaultStruct(structDecl, expr.Type);

        return "undefined";
    }

    private bool TryResolveDefaultType(string describedType, out TgmlTypeDecl? decl)
    {
        var baseTypeName = describedType;
        var genericStart = baseTypeName.IndexOf('<');
        if (genericStart >= 0)
            baseTypeName = baseTypeName[..genericStart];

        while (baseTypeName.EndsWith("[]", StringComparison.Ordinal))
            baseTypeName = baseTypeName[..^2];

        return _ctx.TypeTable.TryResolve(baseTypeName, out decl);
    }

    private string EmitDefaultStruct(TgmlStructDecl structDecl, TgmlTypeRef? explicitType)
    {
        var gmlName = structDecl.QualifiedName?.Replace(".", "_") ?? structDecl.Name;

        if (explicitType is not null && explicitType.TypeArgs.Count > 0)
        {
            var typeArgIds = explicitType.TypeArgs.Select(EmitTypeRefAsRuntimeId);
            return $"new {gmlName}({string.Join(", ", typeArgIds)})";
        }

        return $"new {gmlName}()";
    }

    private string EmitMethodCall(TgmlMethodCallExpr e)
    {
        if (e.Metadata.ContainsKey(DelegateInvokeTargetMetadata))
        {
            var targetExpr = new TgmlFieldAccessExpr
            {
                Target = e.Target,
                FieldName = e.MethodName
            };
            return EmitDelegateInvoke(targetExpr, e, GetNormalizedArgs(e, e.Args));
        }

        var target = Emit(e.Target);
        var args = string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit));

        // If target is the with-alias, strip it → implicit self inside with block
        if (_ctx.WithAlias is { } alias && target == alias)
        {
            return $"{e.MethodName}({args})";
        }

        return $"{target}.{e.MethodName}({args})";
    }

    private string EmitFuncCall(TgmlFuncCallExpr e)
    {
        if (e.Metadata.ContainsKey(DelegateInvokeTargetMetadata))
            return EmitDelegateInvoke(new TgmlIdExpr { Name = e.FunctionName }, e, GetNormalizedArgs(e, e.Args));

        var args = string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit));

        return $"{e.FunctionName}({args})";
    }

    private string EmitFieldAccess(TgmlFieldAccessExpr e)
    {
        if (e.Metadata.TryGetValue(AssetReferenceNameMetadata, out var assetReference) &&
            assetReference is string assetName)
        {
            return assetName;
        }

        if (_ctx.TryResolveStaticAssetReference(e.Target, e.FieldName, out var resolvedAssetName))
        {
            return resolvedAssetName;
        }

        if (TryEmitNativePropertyAccess(e.Target, e.FieldName, out var nativeAccess))
        {
            return nativeAccess;
        }

        var target = Emit(e.Target);
        if (_ctx.WithAlias is { } alias && target == alias)
        {
            return e.FieldName;
        }

        return $"{target}.{e.FieldName}";
    }

    private string EmitIndex(TgmlIndexExpr e)
    {
        if (TryEmitIndexerAccess(e, out var indexerAccess))
            return indexerAccess;

        return $"{Emit(e.Target)}[{Emit(e.Index)}]";
    }

    // ── Property-aware read / write ───────────────────────────────────────────

    /// <summary>
    ///     Emits an identifier in read context. If the name is a property on the current type
    ///     (and we are not already inside that property's own accessor), the read is redirected
    ///     to the GML getter call <c>get_PropName()</c>.
    /// </summary>
    private string EmitIdRead(TgmlIdExpr e)
    {
        if (_ctx.TryGetIdentifierAlias(e.Name, out var alias))
        {
            return alias;
        }

        if (e.Metadata.TryGetValue(AssetReferenceNameMetadata, out var assetReference) &&
            assetReference is string assetName)
        {
            return assetName;
        }

        if (_ctx.TryResolveCurrentTypeAssetReference(e.Name, out var currentTypeAssetName))
        {
            return currentTypeAssetName;
        }

        if (!_ctx.IsInsideAccessorOf(e.Name))
        {
            var prop = _ctx.FindProperty(e.Name);
            if (prop?.Getter is not null)
            {
                if (_ctx.GetNativePropertyName(prop) is { } nativePropertyName)
                {
                    return nativePropertyName;
                }

                return $"get_{e.Name}()";
            }
        }

        return e.Name;
    }

    /// <summary>
    ///     Emits an assignment expression. If the write target resolves to a property on the
    ///     current type, the assignment is redirected to the setter:
    ///     <list type="bullet">
    ///         <item><c>Prop = value</c>  →  <c>set_Prop(value)</c></item>
    ///         <item><c>Prop op= value</c> →  <c>set_Prop(get_Prop() op value)</c></item>
    ///     </list>
    /// </summary>
    private string EmitAssign(TgmlAssignExpr e)
    {
        if (TryEmitIndexerAssignment(e, out var indexerAssignment))
            return indexerAssignment;

        // Don't redirect if we're already inside this property's own accessor
        if (!IsWriteTargetInOwnAccessor(e.Target))
        {
            var (propName, qualifier, prop, nativeTarget) = ExtractPropertyAccess(e.Target);
            if (propName is not null && prop?.Setter is not null)
            {
                var propertyType = DefaultExpressionFacts.DescribeType(prop.Type);
                if (nativeTarget is not null)
                {
                    if (e.Operator == "=")
                    {
                        return $"{nativeTarget} = {Emit(e.Value)}";
                    }

                    var nativeOp = e.Operator[..^1];
                    return $"{nativeTarget} = {nativeTarget} {nativeOp} {Emit(e.Value)}";
                }

                var setPrefix = qualifier is not null ? $"{qualifier}." : string.Empty;
                var setterCall = $"{setPrefix}set_{propName}";
                if (TryEmitDelegateAssignment(propertyType, e.Operator, $"{setPrefix}get_{propName}()", setterCall, e.Value,
                        out var delegateSetterExpr))
                {
                    return delegateSetterExpr;
                }

                if (e.Operator == "=")
                {
                    return $"{setterCall}({Emit(e.Value)})";
                }

                // Compound: op= → set_Prop(get_Prop() op rhs)
                var getterCall = $"{setPrefix}get_{propName}()";
                var op = e.Operator[..^1]; // strip trailing '='
                return $"{setterCall}({getterCall} {op} {Emit(e.Value)})";
            }
        }

        if (TryEmitDelegateAssignment(GetInferredType(e.Target), e.Operator, EmitLValue(e.Target), EmitLValue(e.Target), e.Value,
                out var delegateExpr))
        {
            return delegateExpr;
        }

        // Fall back: emit lvalue directly (bypassing getter redirect) then rhs normally
        return $"{EmitLValue(e.Target)} {e.Operator} {Emit(e.Value)}";
    }

    /// <summary>
    ///     Emits an expression in lvalue (write) context — identifiers are never redirected
    ///     to getters here.
    /// </summary>
    private string EmitLValue(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr e => _ctx.TryGetIdentifierAlias(e.Name, out var alias) ? alias : e.Name,
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: var targetName }, FieldName: var fieldName }
                when _ctx.WithAlias is { } alias && alias == targetName => fieldName,
            TgmlFieldAccessExpr e => $"{EmitLValue(e.Target)}.{e.FieldName}",
            TgmlIndexExpr e => $"{EmitLValue(e.Target)}[{Emit(e.Index)}]",
            _ => Emit(expr)
        };
    }

    /// <summary>
    ///     For assignment targets: returns (<c>propertyName</c>, <c>qualifier</c>) when the
    ///     target is a bare identifier or an explicit <c>self.</c> member that matches a
    ///     property on the current type.  <c>qualifier</c> is <c>null</c> for implicit self.
    /// </summary>
    private (string? propName, string? qualifier, TgmlPropertyDecl? property, string? nativeTarget) ExtractPropertyAccess(TgmlExpression target)
    {
        // Bare name: Name = ...
        if (target is TgmlIdExpr id && _ctx.FindProperty(id.Name) is { } idProperty)
        {
            return (id.Name, null, idProperty, _ctx.GetNativePropertyName(idProperty));
        }

        // self.Name = ...  or  this.Name = ...
        if (target is TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa
            && _ctx.FindProperty(fa.FieldName) is { } fieldProperty)
        {
            return (fa.FieldName, null, fieldProperty, _ctx.GetNativePropertyName(fieldProperty));
        }

        if (target is TgmlFieldAccessExpr fieldAccess &&
            _ctx.FindProperty(fieldAccess.Target, fieldAccess.FieldName) is { } resolvedProperty)
        {
            var isWithAliasTarget =
                _ctx.WithAlias is { } alias &&
                fieldAccess.Target is TgmlIdExpr { Name: var targetName } &&
                targetName == alias;
            var qualifier = isWithAliasTarget ? null : Emit(fieldAccess.Target);
            var nativePropertyName = _ctx.GetNativePropertyName(resolvedProperty);
            var nativeTarget = nativePropertyName is null ? null : $"{qualifier}.{nativePropertyName}";
            if (isWithAliasTarget && nativePropertyName is not null)
                nativeTarget = nativePropertyName;
            return (fieldAccess.FieldName, qualifier, resolvedProperty, nativeTarget);
        }

        return (null, null, null, null);
    }

    private bool TryEmitIndexerAccess(TgmlIndexExpr indexExpr, out string emitted)
    {
        emitted = string.Empty;

        var indexer = _ctx.FindIndexer(indexExpr.Target);
        if (indexer is null)
            return false;

        emitted = $"{BuildIndexerQualifier(indexExpr.Target)}get_Item({Emit(indexExpr.Index)})";
        return true;
    }

    private bool TryEmitIndexerAssignment(TgmlAssignExpr assignExpr, out string emitted)
    {
        emitted = string.Empty;
        if (assignExpr.Target is not TgmlIndexExpr indexExpr)
            return false;

        var indexer = _ctx.FindIndexer(indexExpr.Target);
        if (indexer?.Setter is null)
            return false;

        var qualifier = BuildIndexerQualifier(indexExpr.Target);
        var indexValue = Emit(indexExpr.Index);
        var setterCall = $"{qualifier}set_Item";
        var getterCall = $"{qualifier}get_Item({indexValue})";

        var indexerType = DefaultExpressionFacts.DescribeType(indexer.Type);
        if (TryEmitDelegateIndexerAssignment(indexerType, assignExpr.Operator, setterCall, getterCall, indexValue, assignExpr.Value, out emitted))
            return true;

        if (assignExpr.Operator == "=")
        {
            emitted = $"{setterCall}({indexValue}, {Emit(assignExpr.Value)})";
            return true;
        }

        var op = assignExpr.Operator[..^1];
        emitted = $"{setterCall}({indexValue}, {getterCall} {op} {Emit(assignExpr.Value)})";
        return true;
    }

    private bool TryEmitDelegateIndexerAssignment(
        string delegateType,
        string @operator,
        string setterCall,
        string getterCall,
        string indexValue,
        TgmlExpression valueExpr,
        out string emitted)
    {
        emitted = string.Empty;
        if (!DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out var model))
            return false;

        var rhs = EmitDelegateValue(delegateType, valueExpr);
        if (@operator == "=")
        {
            emitted = $"{setterCall}({indexValue}, {rhs})";
            return true;
        }

        if (@operator == "+=")
        {
            emitted = $"{setterCall}({indexValue}, {model.CombineHelperName}({getterCall}, {rhs}))";
            return true;
        }

        if (@operator == "-=")
        {
            emitted = $"{setterCall}({indexValue}, {model.RemoveHelperName}({getterCall}, {rhs}))";
            return true;
        }

        return false;
    }

    private string BuildIndexerQualifier(TgmlExpression target)
    {
        if (target is TgmlIdExpr { Name: "self" or "this" })
            return string.Empty;

        if (_ctx.WithAlias is { } alias && target is TgmlIdExpr { Name: var targetName } && targetName == alias)
            return string.Empty;

        return $"{Emit(target)}.";
    }

    private bool IsWriteTargetInOwnAccessor(TgmlExpression target)
    {
        var name = target switch
        {
            TgmlIdExpr id => id.Name,
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa => fa.FieldName,
            _ => null
        };
        return name is not null && _ctx.IsInsideAccessorOf(name);
    }

    private bool TryEmitNativePropertyAccess(TgmlExpression target, string propertyName, out string emitted)
    {
        emitted = string.Empty;

        string? nativePropertyName;
        string? qualifier = null;

        if (target is TgmlIdExpr { Name: "self" or "this" })
        {
            nativePropertyName = _ctx.GetNativePropertyName(propertyName);
        }
        else
        {
            var property = _ctx.FindProperty(target, propertyName);
            nativePropertyName = _ctx.GetNativePropertyName(property);
            if (nativePropertyName is not null)
                qualifier = Emit(target);
        }

        if (nativePropertyName is null)
        {
            return false;
        }

        if (target is TgmlIdExpr { Name: "self" or "this" })
        {
            emitted = nativePropertyName;
            return true;
        }

        if (_ctx.WithAlias is { } alias && target is TgmlIdExpr { Name: var targetName } && targetName == alias)
        {
            emitted = nativePropertyName;
            return true;
        }

        if (qualifier is not null)
        {
            emitted = $"{qualifier}.{nativePropertyName}";
            return true;
        }

        return false;
    }

    private string EmitUnary(TgmlUnaryExpr e)
    {
        if (TryGetResolvedOperatorHelper(e, out var operatorHelper))
            return $"{operatorHelper}({Emit(e.Operand)})";

        var op = e.Operator switch
        {
            "not" => "!",
            _ => e.Operator
        };
        return $"{op}{Emit(e.Operand)}";
    }

    private string EmitBinary(TgmlBinaryExpr e)
    {
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
    {
        if (TryGetResolvedConversionHelper(e.Operand, out var conversionHelper))
            return $"{conversionHelper}({EmitCore(e.Operand)})";

        return Emit(e.Operand);
    }

    private string EmitInvoke(TgmlInvokeExpr e)
    {
        return EmitDelegateInvoke(e.Target, e, GetNormalizedArgs(e, e.Args));
    }

    private string EmitIs(TgmlIsExpr e)
    {
        var obj = Emit(e.Operand);
        var typeName = GmlTypeName(e.CheckType);
        return $"(is_struct({obj}) && variable_struct_exists({obj}.__types, __TYPE_{typeName}))";
    }

    private string EmitAs(TgmlAsExpr e)
    {
        var obj = Emit(e.Operand);
        var typeName = GmlTypeName(e.TargetType);
        return $"((is_struct({obj}) && variable_struct_exists({obj}.__types, __TYPE_{typeName})) ? {obj} : undefined)";
    }

    private string EmitNewObject(TgmlNewObjectExpr e)
    {
        var typeName = e.Type.Name.Full;
        _ctx.TypeTable.TryResolve(typeName, out var decl);

        if (decl is TgmlClassDecl cls && cls.IsGameObject)
        {
            // @Object: instance_create_layer pattern
            var objName = _ctx.GmlObjectName(cls);
            var args = GetNormalizedArgs(e, e.Args).Select(Emit).ToList();

            // First 3 args go to instance_create_layer (x, y, layer), rest to Init
            var createArgs = args.Count >= 3
                ? args.Take(3).Concat(new[] { objName })
                : args.Take(args.Count).Concat(new[] { "0", "0", "\"Instances\"", objName }.Skip(3 - args.Count + 1));

            // This returns just the create call; the Init call must be handled at the statement level
            return $"instance_create_layer({string.Join(", ", createArgs)})";
        }

        // Script class: normal constructor
        // If the type is generic, prepend the runtime type-arg IDs (__t0, __t1, ...)
        var gmlName = decl?.QualifiedName?.Replace(".", "_") ?? e.Type.GmlBaseName;
        var typeArgIds = e.Type.TypeArgs.Select(EmitTypeRefAsRuntimeId).ToList();
        var userArgs = GetNormalizedArgs(e, e.Args).Select(Emit).ToList();
        var argsList = string.Join(", ", typeArgIds.Concat(userArgs));
        return $"new {gmlName}({argsList})";
    }

    /// <summary>
    ///     Returns the GML expression for the runtime type ID of a type reference.
    ///     User-defined types map to their <c>__TYPE_X</c> macro; built-in primitives
    ///     use their own macros emitted by <see cref="TypeMacroEmitter" />.
    /// </summary>
    private string EmitTypeRefAsRuntimeId(TgmlTypeRef typeRef)
    {
        var name = typeRef.Name.Full;
        if (_ctx.TypeTable.TryResolve(name, out var decl) && decl?.QualifiedName is { } qn)
            return $"__TYPE_{qn.Replace(".", "_")}";

        // Built-in or unresolved — still emit the macro; TypeMacroEmitter defines built-ins
        return $"__TYPE_{name}";
    }

    private string EmitBaseCall(TgmlBaseCallExpr e)
    {
        var args = string.Join(", ", GetNormalizedArgs(e, e.Args).Select(Emit));

        if (_ctx.CurrentType is TgmlClassDecl cls && cls.IsGameObject)
        {
            return $"undefined /* base.{e.MethodName} is inlined for GameObjects */";
        }

        // In script class → try to inline parent method (best-effort: call parent_Method)
        var parentMethod = e.MethodName;
        if (_ctx.CurrentType is TgmlClassDecl scriptCls)
        {
            var parentTypeName = scriptCls.BaseTypes.FirstOrDefault()?.Name.Full;
            if (parentTypeName is not null)
            {
                _ctx.TypeTable.TryResolve(parentTypeName, out var parentDecl);
                if (parentDecl is TgmlClassDecl parentCls)
                {
                    var parentGml = parentCls.QualifiedName?.Replace(".", "_") ?? parentTypeName;
                    return $"{parentGml}_{parentMethod}(self, {args})".TrimEnd(',', ' ').Replace("(self, )", "(self)");
                }
            }
        }

        return $"/* base.{e.MethodName}({args}) */";
    }

    private string EmitBaseAccess(TgmlBaseAccessExpr e)
    {
        if (_ctx.CurrentType is TgmlClassDecl cls && cls.IsGameObject)
        {
            return e.MemberName; // parent's field, accessible as own field
        }

        return $"/* base.{e.MemberName} */";
    }

    private string EmitLambda(TgmlLambdaExpr e)
    {
        var paramStr = string.Join(", ", e.Params.Select(p => p.Name));
        if (e.ExprBody is not null)
        {
            return $"function({paramStr}) {{ return {Emit(e.ExprBody)}; }}";
        }

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

    private bool TryEmitDelegateConstruction(TgmlExpression expr, out string emitted)
    {
        emitted = string.Empty;

        if (!TryGetDelegateModel(expr, out var model))
            return false;

        string? handlerExpr = null;
        if (expr is TgmlLambdaExpr lambda)
        {
            handlerExpr = EmitLambda(lambda);
        }
        else if (expr.Metadata.ContainsKey(DelegateMethodGroupMetadata))
        {
            handlerExpr = EmitMethodGroupHandler(expr);
        }

        if (handlerExpr is null)
            return false;

        emitted = $"{model.CreateHelperName}({handlerExpr})";
        return true;
    }

    private bool TryEmitDelegateAssignment(
        string? delegateType,
        string @operator,
        string targetExpr,
        string setterTarget,
        TgmlExpression valueExpr,
        out string emitted)
    {
        emitted = string.Empty;
        if (delegateType is null || !DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out var model))
            return false;

        var rhs = EmitDelegateValue(delegateType, valueExpr);
        if (@operator == "=")
        {
            emitted = setterTarget == targetExpr
                ? $"{targetExpr} = {rhs}"
                : $"{setterTarget}({rhs})";
            return true;
        }

        if (@operator == "+=")
        {
            var combined = $"{model.CombineHelperName}({targetExpr}, {rhs})";
            emitted = setterTarget == targetExpr
                ? $"{targetExpr} = {combined}"
                : $"{setterTarget}({combined})";
            return true;
        }

        if (@operator == "-=")
        {
            var removed = $"{model.RemoveHelperName}({targetExpr}, {rhs})";
            emitted = setterTarget == targetExpr
                ? $"{targetExpr} = {removed}"
                : $"{setterTarget}({removed})";
            return true;
        }

        return false;
    }

    private string EmitDelegateValue(string delegateType, TgmlExpression valueExpr)
    {
        if (valueExpr is TgmlNullExpr)
            return "undefined";

        if (TryEmitDelegateConstruction(valueExpr, out var constructed))
            return constructed;

        return Emit(valueExpr);
    }

    private string EmitDelegateInvoke(
        TgmlExpression targetExpr,
        TgmlExpression ownerExpr,
        IReadOnlyList<TgmlExpression> args)
    {
        var targetType = ownerExpr.Metadata.TryGetValue(DelegateInvokeTypeMetadata, out var invokeType) && invokeType is string resolvedType
            ? resolvedType
            : GetInferredType(targetExpr);

        if (targetType is not null && DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, targetType, out var model))
        {
            var emittedArgs = args.Select(Emit).ToList();
            var callArgs = emittedArgs.Count == 0
                ? Emit(targetExpr)
                : $"{Emit(targetExpr)}, {string.Join(", ", emittedArgs)}";
            return $"{model.InvokeHelperName}({callArgs})";
        }

        return $"{Emit(targetExpr)}({string.Join(", ", args.Select(Emit))})";
    }

    private bool TryGetDelegateModel(TgmlExpression expr, out DelegateRuntimeModel model)
    {
        if (expr.Metadata.TryGetValue(ContextualDelegateTypeMetadata, out var delegateTypeValue) &&
            delegateTypeValue is string delegateType &&
            DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out model))
        {
            return true;
        }

        model = null!;
        return false;
    }

    private string EmitMethodGroupHandler(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr id => $"method(self, {id.Name})",
            TgmlFieldAccessExpr fieldAccess => EmitMemberMethodGroupHandler(fieldAccess),
            _ => Emit(expr)
        };
    }

    private string EmitMemberMethodGroupHandler(TgmlFieldAccessExpr expr)
    {
        var target = Emit(expr.Target);
        return $"method({target}, {target}.{expr.FieldName})";
    }

    private static string? GetInferredType(TgmlExpression expr)
    {
        return expr.Metadata.TryGetValue("InferredType", out var inferredType) && inferredType is string typeName
            ? typeName
            : null;
    }
}
