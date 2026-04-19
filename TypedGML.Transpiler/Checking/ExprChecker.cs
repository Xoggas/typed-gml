using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Stateful, symbol-table-aware expression type-checker.
///     Walks a callable body tracking local variable types and verifying:
///     <list type="bullet">
///         <item>Binary / unary operator operand compatibility</item>
///         <item>Assignment target / value type compatibility</item>
///         <item>Return expression type vs. declared return type</item>
///     </list>
///     Type inference is conservative — only literals, casts, locals, and
///     owner-type members are inferred.  Unknown types are silently skipped
///     (no false positives).
/// </summary>
public sealed class ExprChecker
{
    private const string ResolvedOperatorOwnerMetadata = "ResolvedOperatorOwner";
    private const string ResolvedOperatorMethodMetadata = "ResolvedOperatorMethod";
    private const string ResolvedConversionOwnerMetadata = "ResolvedConversionOwner";
    private const string ResolvedConversionMethodMetadata = "ResolvedConversionMethod";
    private const string ContextualDelegateTypeMetadata = "ContextualDelegateType";
    private const string DelegateInvokeTargetMetadata = "DelegateInvokeTarget";
    private const string DelegateInvokeTypeMetadata = "DelegateInvokeType";
    private const string DelegateMethodGroupMetadata = "DelegateMethodGroup";
    private const string AssetReferenceNameMetadata = AssetFacts.AssetReferenceNameMetadata;

    private readonly TranspileContext _ctx;
    private readonly TgmlFile _file;
    private readonly TgmlTypeDecl? _owner;
    private readonly string _returnType; // "void" or declared return type name

    public ExprChecker(
        TranspileContext ctx,
        TgmlFile file,
        SymbolTable symbols,
        TgmlTypeDecl? owner = null,
        string returnType = "void")
    {
        _ctx = ctx;
        _file = file;
        Symbols = symbols;
        _owner = owner;
        _returnType = returnType;
    }

    public SymbolTable Symbols { get; }

    // ── Statement walker ──────────────────────────────────────────────────────

    public void CheckBlock(TgmlBlock block)
    {
        Symbols.PushScope();
        foreach (var stmt in block.Statements)
            CheckStmt(stmt);
        Symbols.PopScope();
    }

    public void CheckStmt(TgmlStatement stmt)
    {
        switch (stmt)
        {
            case TgmlBlock b:
                CheckBlock(b);
                break;

            case TgmlLocalVarDecl v:
                if (v.IsImplicitlyTyped)
                    DefineImplicitLocal(v.Name, v.Initializer, v.Line, v.Column, "local variable");
                else
                {
                    if (v.Initializer is not null)
                        CheckAssignCompatibility(DefaultExpressionFacts.DescribeType(v.Type), v.Initializer,
                            v.Line, v.Column, $"Cannot assign '{{1}}' to variable of type '{v.Type.Name.Full}'.");
                    Symbols.Define(v.Name, v.Type);
                }
                break;

            case TgmlExpressionStmt es:
                CheckExpr(es.Expression);
                break;

            case TgmlIfStmt i:
                foreach (var br in i.Branches)
                {
                    CheckExpr(br.Condition);
                    CheckBlock(br.Body);
                }

                if (i.ElseBlock is not null) CheckBlock(i.ElseBlock);
                break;

            case TgmlWhileStmt w:
                CheckExpr(w.Condition);
                CheckBlock(w.Body);
                break;

            case TgmlForStmt f:
                Symbols.PushScope();
                switch (f.Init)
                {
                    case TgmlForInitDecl d:
                        if (d.IsImplicitlyTyped)
                            DefineImplicitLocal(d.Name, d.Initializer, f.Line, f.Column, "for-loop variable");
                        else
                        {
                            if (d.Initializer is not null)
                                CheckAssignCompatibility(DefaultExpressionFacts.DescribeType(d.Type), d.Initializer,
                                    f.Line, f.Column, $"Cannot assign '{{1}}' to variable of type '{d.Type.Name.Full}'.");
                            Symbols.Define(d.Name, d.Type);
                        }
                        break;
                    case TgmlForInitExpr e:
                        CheckExpr(e.Expression);
                        break;
                }

                if (f.Condition is not null) CheckExpr(f.Condition);
                foreach (var u in f.Updates) CheckExpr(u);
                foreach (var s in f.Body.Statements) CheckStmt(s);
                Symbols.PopScope();
                break;

            case TgmlRepeatStmt r:
                CheckExpr(r.Count);
                CheckBlock(r.Body);
                break;

            case TgmlSwitchStmt sw:
                CheckExpr(sw.Value);
                foreach (var sec in sw.Sections)
                {
                    if (sec.CaseValue is not null) CheckExpr(sec.CaseValue);
                    foreach (var s in sec.Statements) CheckStmt(s);
                }

                break;

            case TgmlWithStmt withStmt:
                Symbols.PushScope();
                Symbols.Define(withStmt.VarName, withStmt.IterType);
                CheckBlock(withStmt.Body);
                Symbols.PopScope();
                break;

            case TgmlReturnStmt ret:
                CheckReturn(ret);
                break;

            case TgmlTryStmt tr:
                CheckBlock(tr.TryBlock);
                foreach (var cc in tr.CatchClauses)
                {
                    var inner = new ExprChecker(_ctx, _file, Symbols, _owner, _returnType);
                    inner.Symbols.PushScope();
                    inner.Symbols.Define(cc.VarName, cc.ExceptionType);
                    foreach (var s in cc.Body.Statements) inner.CheckStmt(s);
                    inner.Symbols.PopScope();
                }

                if (tr.FinallyBlock is not null) CheckBlock(tr.FinallyBlock);
                break;
        }
    }

    // ── Return checking ───────────────────────────────────────────────────────

    private void CheckReturn(TgmlReturnStmt ret)
    {
        if (_returnType == "void")
        {
            if (ret.Value is not null)
            {
                CheckExpr(ret.Value);
                Error(ret.Line, ret.Column, "Cannot return a value from a void method.");
            }

            return;
        }

        if (ret.Value is null)
        {
            // bare return; in non-void context — warn only (branch might be unreachable)
            _ctx.AddWarning($"Missing return value; method declares return type '{_returnType}'.",
                _file.FileName, ret.Line, ret.Column);
            return;
        }

        CheckAssignCompatibility(_returnType, ret.Value, ret.Line, ret.Column,
            $"Return type mismatch: cannot convert '{{1}}' to '{_returnType}'.");
    }

    // ── Expression type-checker ───────────────────────────────────────────────

    public void CheckExpr(TgmlExpression expr) => InferType(expr);

    /// <summary>
    ///     Returns the inferred primitive type of <paramref name="expr" />, or
    ///     <c>null</c> when the type cannot be statically determined.
    /// </summary>
    public string? InferType(TgmlExpression expr)
    {
        var inferred = expr switch
        {
            TgmlIntLiteralExpr => "number",
            TgmlRealLiteralExpr => "number",
            TgmlBoolLiteralExpr => "bool",
            TgmlStringLiteralExpr => "string",
            TgmlNullExpr => "null",
            TgmlDefaultExpr d => InferDefault(d),
            TgmlParenExpr p => InferType(p.Inner),
            TgmlCastExpr c => InferCast(c),
            TgmlIdExpr id => InferIdType(id),
            TgmlValueKeywordExpr => InferKeywordVar("value"),
            TgmlFieldKeywordExpr => InferKeywordVar("field"),
            TgmlUnaryExpr u => InferUnary(u),
            TgmlBinaryExpr b => InferBinary(b),
            TgmlTernaryExpr t => InferTernary(t),
            TgmlAssignExpr a => InferAssign(a),
            TgmlArrayInitExpr ai => VisitArrayInit(ai),
            TgmlMethodCallExpr mc => VisitMethodCall(mc),
            TgmlFuncCallExpr fc => VisitFuncCall(fc),
            TgmlNewObjectExpr no => VisitNewObject(no),
            TgmlBaseCallExpr bc => VisitBaseCall(bc),
            TgmlNewArrayExpr na => VisitNewArray(na),
            TgmlIndexExpr ix => VisitIndex(ix),
            TgmlInvokeExpr invoke => VisitInvoke(invoke),
            TgmlFieldAccessExpr fa => VisitFieldAccess(fa),
            TgmlLambdaExpr lam => VisitLambda(lam),
            _ => null
        };

        if (inferred is null)
            expr.Metadata.Remove("InferredType");
        else
            expr.Metadata["InferredType"] = inferred;

        return inferred;
    }

    // ── Inference helpers ─────────────────────────────────────────────────────

    private string? InferIdType(TgmlIdExpr id)
    {
        // 1. Local variable / parameter in scope
        if (Symbols.TryResolve(id.Name, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);

        // 1b. Instance self-reference
        if (id.Name is "self" or "this")
            return _owner?.QualifiedName ?? _owner?.Name;

        // 2. Property on the owning type / base types
        var prop = ResolvePropertyOnCurrentType(id.Name);
        if (prop is not null)
        {
            EnsureReadable(prop, id);
            if (prop.Property.IsStatic && AssetFacts.TryGetAssetName(prop.Property, out var assetName))
                id.Metadata[AssetReferenceNameMetadata] = assetName;
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);
        }

        // 3. Field on the owning type
        var field = FindFieldInHierarchy(_owner, id.Name);
        if (field is not null && field.IsStatic && AssetFacts.TryGetAssetName(field, out var fieldAssetName))
            id.Metadata[AssetReferenceNameMetadata] = fieldAssetName;
        return field is null ? null : DefaultExpressionFacts.DescribeType(field.Type);
    }

    /// <summary>
    ///     Resolves a contextual-keyword identifier (<c>value</c> / <c>field</c>) that may
    ///     also be used as a local variable name.
    /// </summary>
    private string? InferKeywordVar(string keyword)
    {
        if (Symbols.TryResolve(keyword, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);
        return null;
    }

    private string? InferUnary(TgmlUnaryExpr expr)
    {
        var t = InferType(expr.Operand);
        if (t is null) return null;

        ClearResolvedOperator(expr);
        if (TryResolveUnaryOperator(expr.Operator, expr.Operand, t, out var resolvedUnaryOwner, out var resolvedUnary))
        {
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedUnary.Params[0].Type), expr.Operand);
            SetResolvedOperator(expr, resolvedUnaryOwner, resolvedUnary);
            return DefaultExpressionFacts.DescribeType(resolvedUnary.ReturnType);
        }

        switch (expr.Operator)
        {
            case "+":
            case "-":
                if (!TypeCompatibility.IsNumeric(t))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to type '{t}'.");
                return TypeCompatibility.IsNumeric(t) ? t : null;

            case "~":
                if (!TypeCompatibility.IsNumeric(t))
                    Error(expr, $"Operator '~' requires 'number', got '{t}'.");
                return TypeCompatibility.IsNumeric(t) ? "number" : null;

            case "not":
                if (t != "bool")
                    Error(expr, $"Operator 'not' requires 'bool', got '{t}'.");
                return "bool";
        }

        return null;
    }

    private string? InferBinary(TgmlBinaryExpr expr)
    {
        var lt = InferType(expr.Left);
        var rt = InferType(expr.Right);

        if (lt is null || rt is null) return null;

        ClearResolvedOperator(expr);
        if (TryResolveBinaryOperator(expr.Operator, expr.Left, expr.Right, lt, rt, out var resolvedBinaryOwner, out var resolvedBinary))
        {
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedBinary.Params[0].Type), expr.Left);
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedBinary.Params[1].Type), expr.Right);
            SetResolvedOperator(expr, resolvedBinaryOwner, resolvedBinary);
            return DefaultExpressionFacts.DescribeType(resolvedBinary.ReturnType);
        }

        switch (expr.Operator)
        {
            case "+" or "-" or "*" or "/" or "%":
                if (expr.Operator == "+" && lt == "string" && rt == "string") return "string";
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return TypeCompatibility.IsNumeric(lt) && TypeCompatibility.IsNumeric(rt) ? "number" : null;

            case "&" or "|" or "^" or "<<" or ">>":
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr,
                        $"Bitwise operator '{expr.Operator}' requires 'number' operands, got '{lt}' and '{rt}'.");
                return TypeCompatibility.IsNumeric(lt) && TypeCompatibility.IsNumeric(rt) ? "number" : null;

            case "and" or "or" or "&&" or "||":
                if (lt != "bool" || rt != "bool")
                    Error(expr,
                        $"Logical operator '{expr.Operator}' requires 'bool' operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "<" or ">" or "<=" or ">=":
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr,
                        $"Relational operator '{expr.Operator}' requires numeric operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "==" or "!=":
                if (lt != "null" && rt != "null" && !AreTypesComparable(lt, rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return "bool";
        }

        return null;
    }

    private string? InferCast(TgmlCastExpr expr)
    {
        var targetType = DefaultExpressionFacts.DescribeType(expr.TargetType);
        DefaultExpressionFacts.TryApplyContextualType(expr.Operand, targetType);

        if (!CanConvertExpression(targetType, expr.Operand, allowExplicit: true, apply: true))
        {
            var sourceType = InferType(expr.Operand);
            if (sourceType is not null)
                Error(expr, $"Cannot convert type '{sourceType}' to '{targetType}'.");
        }

        return targetType;
    }

    private string? InferTernary(TgmlTernaryExpr expr)
    {
        CheckExpr(expr.Condition);
        var tt = InferType(expr.ThenExpr);
        var et = InferType(expr.ElseExpr);
        return tt == et ? tt : null;
    }

    private string? InferDefault(TgmlDefaultExpr expr)
    {
        return DefaultExpressionFacts.GetEffectiveTypeName(expr);
    }

    private string? InferAssign(TgmlAssignExpr expr)
    {
        CheckAssignmentTarget(expr.Target);
        if (TryResolveAssetAccess(expr.Target, out var assetName, out var assetType))
        {
            Error(expr, $"Asset reference '{assetName}' is read-only and cannot be assigned.");
            CheckExpr(expr.Value);
            return assetType;
        }

        var lt = InferLValueType(expr.Target);
        if (lt is not null)
            expr.Target.Metadata["InferredType"] = lt;

        if (lt is not null &&
            DelegateFacts.IsDelegateType(_ctx.TypeTable, lt) &&
            expr.Operator is "+=" or "-=")
        {
            CheckPropertyWrite(expr.Target, requireRead: true);
            DefaultExpressionFacts.TryApplyContextualType(expr.Value, lt);
            if (!CanConvertExpression(lt, expr.Value, allowExplicit: false, apply: true))
            {
                var delegateValueType = InferType(expr.Value);
                Error(expr, delegateValueType is not null
                    ? $"Cannot assign '{delegateValueType}' to '{lt}'."
                    : $"Cannot convert the supplied expression to delegate type '{lt}'.");
            }
            else
            {
                CheckExpr(expr.Value);
            }
        }
        else if (expr.Operator != "=")
        {
            var rt = InferType(expr.Value);
            CheckPropertyWrite(expr.Target, requireRead: true);
            if (lt is not null && rt is not null)
                InferBinary(new TgmlBinaryExpr
                    { Left = expr.Target, Operator = expr.Operator[..^1], Right = expr.Value, Line = expr.Line, Column = expr.Column });
        }
        else
        {
            if (lt is not null)
                DefaultExpressionFacts.TryApplyContextualType(expr.Value, lt);
            var rt = InferType(expr.Value);
            CheckPropertyWrite(expr.Target, requireRead: false);
            if (lt is not null && !CanConvertExpression(lt, expr.Value, allowExplicit: false, apply: true))
                Error(expr, rt is not null
                    ? $"Cannot assign '{rt}' to '{lt}'."
                    : $"Cannot convert the supplied expression to '{lt}'.");
            else
                CheckExpr(expr.Value);
        }

        return lt;
    }

    // ── Visitor stubs (recurse, return null) ─────────────────────────────────

    private string? VisitArrayInit(TgmlArrayInitExpr expr)
    {
        foreach (var el in expr.Elements) CheckExpr(el);
        return null;
    }

    private string? VisitMethodCall(TgmlMethodCallExpr mc)
    {
        var targetType = InferType(mc.Target);
        foreach (var a in mc.Args) CheckExpr(a.Value);

        if (targetType is null ||
            !_ctx.TypeTable.TryResolve(targetType, out var targetDecl) ||
            targetDecl is null)
        {
            if (mc.Args.Any(a => a.Name is not null))
                Error(mc, $"Named arguments cannot be used because method '{mc.MethodName}' could not be resolved.");
            return null;
        }

        var candidates = FindMethodsInHierarchy(targetDecl, mc.MethodName);
        if (candidates.Count == 0)
        {
            if (TryResolveDelegateMemberInvoke(mc, targetDecl, out var delegateReturnType))
                return delegateReturnType;

            if (mc.Args.Any(a => a.Name is not null))
                Error(mc, $"Named arguments cannot be used because method '{mc.MethodName}' could not be resolved.");
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
        return DefaultExpressionFacts.DescribeType(resolvedMethod!.ReturnType);
    }

    private string VisitNewObject(TgmlNewObjectExpr no)
    {
        foreach (var a in no.Args) CheckExpr(a.Value);

        if (_ctx.TypeTable.TryResolve(no.Type.Name.Full, out var decl) && decl is TgmlClassDecl or TgmlStructDecl)
        {
            var constructors = decl switch
            {
                TgmlClassDecl cls => cls.Constructors.Cast<TgmlConstructorDecl>().ToList(),
                TgmlStructDecl str => str.Constructors.Cast<TgmlConstructorDecl>().ToList(),
                _ => []
            };

            if (constructors.Count == 0)
            {
                if (no.Args.Count > 0)
                    Error(no, $"Type '{no.Type.Name.Full}' does not define a constructor that accepts arguments.");
            }
            else if (!CallArgumentBinder.TryResolveOverload(
                         constructors,
                         c => c.Params,
                         no.Args,
                         InferType,
                         CanAssignImplicitly,
                         out _,
                         out var bound,
                         out var error))
            {
                Error(no, $"No constructor overload for '{no.Type.Name.Full}' matches the supplied arguments: {error}");
            }
            else
            {
                no.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
                ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
            }
        }

        return DefaultExpressionFacts.DescribeType(no.Type);
    }

    private string? VisitBaseCall(TgmlBaseCallExpr bc)
    {
        foreach (var a in bc.Args) CheckExpr(a.Value);

        if (_owner is not TgmlClassDecl ownerClass)
            return null;

        var baseClass = ResolveBaseClass(ownerClass);
        if (baseClass is null)
            return null;

        var candidates = FindMethodsInHierarchy(baseClass, bc.MethodName);
        if (candidates.Count == 0)
        {
            if (bc.Args.Any(a => a.Name is not null))
                Error(bc, $"Named arguments cannot be used because base method '{bc.MethodName}' could not be resolved.");
            return null;
        }

        if (!CallArgumentBinder.TryResolveOverload(
                candidates,
                m => m.Params,
                bc.Args,
                InferType,
                CanAssignImplicitly,
                out var resolvedMethod,
                out var bound,
                out var error))
        {
            Error(bc, $"No base-method overload '{bc.MethodName}' matches the supplied arguments: {error}");
            return null;
        }

        bc.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        return DefaultExpressionFacts.DescribeType(resolvedMethod!.ReturnType);
    }

    private string? VisitNewArray(TgmlNewArrayExpr na)
    {
        CheckExpr(na.Size);
        return null;
    }

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

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        var prop = PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, fa.FieldName);
        if (prop is not null)
        {
            EnsureReadable(prop, fa);
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);
        }

        var field = FindFieldInHierarchy(targetDecl, fa.FieldName);
        return field is null ? null : DefaultExpressionFacts.DescribeType(field.Type);
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

    private bool ValidateLambdaAgainstDelegate(TgmlLambdaExpr lambda, DelegateSignature signature, bool apply)
    {
        if (lambda.Params.Count != signature.Parameters.Count)
            return false;

        var paramBindings = new List<TgmlParam>(signature.Parameters.Count);
        for (var i = 0; i < signature.Parameters.Count; i++)
        {
            var lambdaParam = lambda.Params[i];
            var delegateParam = signature.Parameters[i];
            var lambdaParamType = DefaultExpressionFacts.DescribeType(lambdaParam.Type);
            if (lambdaParamType != "?" && lambdaParamType != delegateParam.TypeName)
                return false;

            paramBindings.Add(new TgmlParam
            {
                Name = lambdaParam.Name,
                Type = MakeTypeRef(delegateParam.TypeName),
                Decorators = lambdaParam.Decorators
            });
        }

        if (!apply)
            return true;

        if (apply)
            lambda.Metadata[ContextualDelegateTypeMetadata] = signature.TypeName;

        var inner = new ExprChecker(_ctx, _file, Symbols, _owner, signature.ReturnType);
        inner.Symbols.PushScope();
        foreach (var binding in paramBindings)
            inner.Symbols.Define(binding.Name, binding.Type);

        if (lambda.ExprBody is not null)
        {
            if (signature.ReturnType == "void")
            {
                inner.Error(lambda, "Expression-bodied lambda cannot be converted to a void delegate.");
                inner.Symbols.PopScope();
                return true;
            }

            inner.CheckAssignCompatibility(signature.ReturnType, lambda.ExprBody, lambda.Line, lambda.Column,
                $"Lambda return type mismatch: cannot convert '{{1}}' to '{signature.ReturnType}'.");
        }
        else if (lambda.BlockBody is not null)
        {
            inner.CheckBlock(lambda.BlockBody);
        }

        inner.Symbols.PopScope();
        return true;
    }

    private bool CanAssignImplicitly(string targetType, TgmlExpression expr)
        => CanConvertExpression(targetType, expr, allowExplicit: false, apply: false);

    private void ApplyBoundArgumentConversions(
        IReadOnlyList<TgmlParam> parameters,
        IReadOnlyList<TgmlExpression> arguments)
    {
        for (var i = 0; i < Math.Min(parameters.Count, arguments.Count); i++)
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(parameters[i].Type), arguments[i]);
    }

    private bool ApplyImplicitConversion(string targetType, TgmlExpression expr)
        => CanConvertExpression(targetType, expr, allowExplicit: false, apply: true);

    private bool CanConvertExpression(string targetType, TgmlExpression expr, bool allowExplicit, bool apply)
    {
        DefaultExpressionFacts.TryApplyContextualType(expr, targetType);

        if (expr is TgmlLambdaExpr lambda && DelegateFacts.TryResolveSignature(_ctx.TypeTable, targetType, out var delegateSignature))
            return ValidateLambdaAgainstDelegate(lambda, delegateSignature, apply);

        if (DelegateFacts.TryResolveSignature(_ctx.TypeTable, targetType, out var targetDelegateSignature) &&
            TryResolveMethodGroup(targetDelegateSignature, expr, apply, out _))
        {
            if (apply)
                expr.Metadata[ContextualDelegateTypeMetadata] = targetType;
            return true;
        }

        var sourceType = InferType(expr);
        if (sourceType is null)
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        if (CanAssignTypeToType(targetType, sourceType))
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        if (TryResolveUserDefinedConversion(sourceType, targetType, allowExplicit, out var conversionOwner, out var conversionMethod))
        {
            if (apply)
                SetResolvedConversion(expr, conversionOwner, conversionMethod);
            return true;
        }

        if (allowExplicit && CanExplicitlyCastTypeToType(targetType, sourceType))
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        return false;
    }

    private bool TryResolveMethodGroup(
        DelegateSignature targetSignature,
        TgmlExpression expr,
        bool apply,
        out TgmlMethodDecl? resolvedMethod)
    {
        expr.Metadata.Remove(DelegateMethodGroupMetadata);
        resolvedMethod = null;

        var candidates = expr switch
        {
            TgmlIdExpr id => ResolveBareMethodGroupCandidates(id),
            TgmlFieldAccessExpr fieldAccess => ResolveMemberMethodGroupCandidates(fieldAccess),
            _ => []
        };

        if (candidates.Count == 0)
            return false;

        var compatible = candidates
            .Where(method => MethodMatchesDelegateSignature(method, targetSignature))
            .ToList();

        if (compatible.Count == 0)
        {
            if (apply)
                Error(expr, $"Method group '{DescribeMethodGroup(expr)}' does not match delegate type '{targetSignature.TypeName}'.");
            return false;
        }

        var ranked = compatible
            .Select(method => (Method: method, Score: ScoreMethodGroupCandidate(method, targetSignature)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            if (apply)
                Error(expr, $"Method group '{DescribeMethodGroup(expr)}' is ambiguous for delegate type '{targetSignature.TypeName}'.");
            return false;
        }

        resolvedMethod = ranked[0].Method;
        if (apply)
            expr.Metadata[DelegateMethodGroupMetadata] = true;
        return true;
    }

    private List<TgmlMethodDecl> ResolveBareMethodGroupCandidates(TgmlIdExpr id)
    {
        if (Symbols.TryResolve(id.Name, out _))
            return [];

        if (ResolvePropertyOnCurrentType(id.Name) is not null)
            return [];

        if (FindFieldInHierarchy(_owner, id.Name) is not null)
            return [];

        return FindMethodsInHierarchy(_owner, id.Name)
            .Where(IsMethodGroupCandidate)
            .ToList();
    }

    private List<TgmlMethodDecl> ResolveMemberMethodGroupCandidates(TgmlFieldAccessExpr fieldAccess)
    {
        var targetType = InferType(fieldAccess.Target);
        if (targetType is null ||
            !_ctx.TypeTable.TryResolve(targetType, out var targetDecl) ||
            targetDecl is null)
        {
            return [];
        }

        return FindMethodsInHierarchy(targetDecl, fieldAccess.FieldName)
            .Where(IsMethodGroupCandidate)
            .ToList();
    }

    private static bool IsMethodGroupCandidate(TgmlMethodDecl method)
    {
        return !method.IsAbstract &&
               !method.IsUserDefinedOperator &&
               method.TypeParams.Count == 0;
    }

    private bool MethodMatchesDelegateSignature(TgmlMethodDecl method, DelegateSignature signature)
    {
        if (method.Params.Count != signature.Parameters.Count)
            return false;

        for (var i = 0; i < method.Params.Count; i++)
        {
            var methodParamType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            var delegateParamType = signature.Parameters[i].TypeName;
            if (!CanAssignTypeToType(methodParamType, delegateParamType))
                return false;
        }

        var methodReturnType = DefaultExpressionFacts.DescribeType(method.ReturnType);
        return CanAssignTypeToType(signature.ReturnType, methodReturnType);
    }

    private static int ScoreMethodGroupCandidate(TgmlMethodDecl method, DelegateSignature signature)
    {
        var score = 0;
        for (var i = 0; i < Math.Min(method.Params.Count, signature.Parameters.Count); i++)
        {
            var methodParamType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            if (methodParamType == signature.Parameters[i].TypeName)
                score += 2;
            else
                score += 1;
        }

        if (DefaultExpressionFacts.DescribeType(method.ReturnType) == signature.ReturnType)
            score += 2;

        return score;
    }

    private static string DescribeMethodGroup(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr id => id.Name,
            TgmlFieldAccessExpr fieldAccess => fieldAccess.FieldName,
            _ => "expression"
        };
    }

    private bool CanAssignTypeToType(string targetType, string sourceType)
    {
        if (TypeCompatibility.AreAssignable(targetType, sourceType))
            return true;

        if (DelegateFacts.IsDelegateType(_ctx.TypeTable, targetType) || DelegateFacts.IsDelegateType(_ctx.TypeTable, sourceType))
            return targetType == sourceType;

        if (targetType == "any")
            return true;

        if (targetType == "object")
            return sourceType != "void";

        if (targetType == "struct")
            return TryResolveTypeDecl(sourceType, out var structDecl) && structDecl is TgmlStructDecl;

        return IsReferenceAssignable(targetType, sourceType);
    }

    private bool CanExplicitlyCastTypeToType(string targetType, string sourceType)
    {
        if (CanAssignTypeToType(targetType, sourceType) || CanAssignTypeToType(sourceType, targetType))
            return true;

        if (BuiltinTypeFacts.IsPrimitive(targetType) || BuiltinTypeFacts.IsPrimitive(sourceType))
            return false;

        if (sourceType == "null")
            return true;

        if (!TryResolveTypeDecl(targetType, out var targetDecl) || !TryResolveTypeDecl(sourceType, out var sourceDecl))
            return false;

        if (sourceDecl is TgmlClassDecl && targetDecl is TgmlStructDecl)
            return false;

        if (sourceDecl is TgmlStructDecl && targetDecl is TgmlClassDecl)
            return false;

        return sourceDecl is TgmlClassDecl or TgmlInterfaceDecl &&
               targetDecl is TgmlClassDecl or TgmlInterfaceDecl;
    }

    private bool AreTypesComparable(string leftType, string rightType)
    {
        return TypeCompatibility.AreComparable(leftType, rightType) ||
               CanAssignTypeToType(leftType, rightType) ||
               CanAssignTypeToType(rightType, leftType);
    }

    private bool IsReferenceAssignable(string targetType, string sourceType)
    {
        if (!TryResolveTypeDecl(targetType, out var targetDecl) ||
            !TryResolveTypeDecl(sourceType, out var sourceDecl) ||
            targetDecl is null ||
            sourceDecl is null)
            return false;

        if ((targetDecl.QualifiedName is not null && targetDecl.QualifiedName == sourceDecl.QualifiedName) ||
            targetDecl.Name == sourceDecl.Name)
            return true;

        var reachable = CollectReachableTypeNames(sourceDecl, new HashSet<string>(StringComparer.Ordinal));
        return reachable.Contains(targetType) ||
               (targetDecl.QualifiedName is not null && reachable.Contains(targetDecl.QualifiedName)) ||
               reachable.Contains(targetDecl.Name);
    }

    private bool TryResolveUserDefinedConversion(
        string sourceType,
        string targetType,
        bool allowExplicit,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        var candidates = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        CollectConversionCandidates(sourceType, sourceType, targetType, allowExplicit, candidates, seen);
        if (targetType != sourceType)
            CollectConversionCandidates(targetType, sourceType, targetType, allowExplicit, candidates, seen);

        if (candidates.Count == 0)
        {
            owner = null!;
            method = null!;
            return false;
        }

        var ranked = candidates
            .Select(candidate => (candidate.Owner, candidate.Method, Score: ScoreConversionCandidate(candidate.Method, sourceType, targetType)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            owner = null!;
            method = null!;
            return false;
        }

        owner = ranked[0].Owner;
        method = ranked[0].Method;
        return true;
    }

    private void CollectConversionCandidates(
        string lookupType,
        string sourceType,
        string targetType,
        bool allowExplicit,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results,
        HashSet<string> seen)
    {
        if (!TryResolveTypeDecl(lookupType, out var ownerDecl))
            return;

        foreach (var candidate in CollectMethodsInHierarchy(ownerDecl, method => method.IsConversionOperator))
        {
            if ((!allowExplicit && candidate.Method.Conversion != ConversionModifier.Implicit) ||
                candidate.Method.Params.Count != 1)
            {
                continue;
            }

            var paramType = DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type);
            var returnType = DefaultExpressionFacts.DescribeType(candidate.Method.ReturnType);
            if (!CanAssignTypeToType(paramType, sourceType) ||
                !CanAssignTypeToType(targetType, returnType))
            {
                continue;
            }

            var key = $"{candidate.Owner.QualifiedName ?? candidate.Owner.Name}:{OperatorFacts.GetHelperName(candidate.Owner, candidate.Method)}";
            if (seen.Add(key))
                results.Add(candidate);
        }
    }

    private int ScoreConversionCandidate(TgmlMethodDecl method, string sourceType, string targetType)
    {
        var score = 0;
        var paramType = DefaultExpressionFacts.DescribeType(method.Params[0].Type);
        var returnType = DefaultExpressionFacts.DescribeType(method.ReturnType);

        if (paramType == sourceType)
            score += 2;

        if (returnType == targetType)
            score += 2;

        if (method.Conversion == ConversionModifier.Implicit)
            score += 1;

        return score;
    }

    private bool TryResolveUnaryOperator(
        string operatorToken,
        TgmlExpression operand,
        string operandType,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        if (!TryResolveTypeDecl(operandType, out var operandDecl))
        {
            owner = null!;
            method = null!;
            return false;
        }

        var candidates = CollectMethodsInHierarchy(
                operandDecl,
                candidate => candidate.IsOperatorOverload &&
                             candidate.OperatorToken == operatorToken &&
                             candidate.Params.Count == 1)
            .Where(candidate => CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type), operandType))
            .ToList();

        return TryChooseOperatorCandidate(candidates, [operand], out owner, out method);
    }

    private bool TryResolveBinaryOperator(
        string operatorToken,
        TgmlExpression left,
        TgmlExpression right,
        string leftType,
        string rightType,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        var candidates = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        CollectOperatorCandidates(leftType, operatorToken, seen, candidates);
        if (rightType != leftType)
            CollectOperatorCandidates(rightType, operatorToken, seen, candidates);

        candidates = candidates
            .Where(candidate =>
                candidate.Method.Params.Count == 2 &&
                CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type), leftType) &&
                CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[1].Type), rightType))
            .ToList();

        return TryChooseOperatorCandidate(candidates, [left, right], out owner, out method);
    }

    private void CollectOperatorCandidates(
        string lookupType,
        string operatorToken,
        HashSet<string> seen,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results)
    {
        if (!TryResolveTypeDecl(lookupType, out var ownerDecl))
            return;

        foreach (var candidate in CollectMethodsInHierarchy(
                     ownerDecl,
                     method => method.IsOperatorOverload && method.OperatorToken == operatorToken))
        {
            var key = $"{candidate.Owner.QualifiedName ?? candidate.Owner.Name}:{OperatorFacts.GetHelperName(candidate.Owner, candidate.Method)}";
            if (seen.Add(key))
                results.Add(candidate);
        }
    }

    private bool TryChooseOperatorCandidate(
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> candidates,
        IReadOnlyList<TgmlExpression> operands,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        if (candidates.Count == 0)
        {
            owner = null!;
            method = null!;
            return false;
        }

        var ranked = candidates
            .Select(candidate => (candidate.Owner, candidate.Method, Score: ScoreOperatorCandidate(candidate.Method, operands)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            owner = null!;
            method = null!;
            return false;
        }

        owner = ranked[0].Owner;
        method = ranked[0].Method;
        return true;
    }

    private int ScoreOperatorCandidate(TgmlMethodDecl method, IReadOnlyList<TgmlExpression> operands)
    {
        var score = 0;
        for (var i = 0; i < Math.Min(method.Params.Count, operands.Count); i++)
        {
            var operandType = InferType(operands[i]);
            var paramType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            if (operandType == paramType)
                score += 2;
            else if (operandType is not null && CanAssignTypeToType(paramType, operandType))
                score += 1;
        }

        return score;
    }

    private List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> CollectMethodsInHierarchy(
        TgmlTypeDecl? type,
        Func<TgmlMethodDecl, bool> predicate)
    {
        var results = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        CollectMethodsInHierarchy(type, predicate, new HashSet<string>(StringComparer.Ordinal), results);
        return results;
    }

    private void CollectMethodsInHierarchy(
        TgmlTypeDecl? type,
        Func<TgmlMethodDecl, bool> predicate,
        HashSet<string> visited,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results)
    {
        if (type is null)
            return;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key))
            return;

        switch (type)
        {
            case TgmlClassDecl cls:
                results.AddRange(cls.Methods.Where(predicate).Select(method => ((TgmlTypeDecl)cls, method)));
                foreach (var baseRef in cls.BaseTypes)
                {
                    if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                        CollectMethodsInHierarchy(baseDecl, predicate, visited, results);
                }
                break;

            case TgmlStructDecl str:
                results.AddRange(str.Methods.Where(predicate).Select(method => ((TgmlTypeDecl)str, method)));
                foreach (var baseRef in str.BaseTypes)
                {
                    if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                        CollectMethodsInHierarchy(baseDecl, predicate, visited, results);
                }
                break;
        }
    }

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

        if (bases is null)
            return visited;

        foreach (var baseRef in bases)
        {
            visited.Add(baseRef.Name.Full);
            if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                CollectReachableTypeNames(baseDecl, visited);
        }

        return visited;
    }

    private bool TryResolveTypeDecl(string typeName, out TgmlTypeDecl? decl)
    {
        var (baseTypeName, typeArgs) = DelegateFacts.SplitDescribedType(typeName);
        var typeArgCount = typeArgs.Count;

        while (baseTypeName.EndsWith("[]", StringComparison.Ordinal))
            baseTypeName = baseTypeName[..^2];

        return _ctx.TypeTable.TryResolve(baseTypeName, typeArgCount, out decl);
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

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>Checks that <paramref name="valueExpr" />'s type is assignable to <paramref name="targetType" />.</summary>
    public void CheckAssignCompatibility(string targetType, TgmlExpression valueExpr, int line, int col,
        string messageTemplate)
    {
        DefaultExpressionFacts.TryApplyContextualType(valueExpr, targetType);

        // InferType already recurses into all sub-expressions and fires any operand errors.
        var inferred = InferType(valueExpr);
        if (!CanConvertExpression(targetType, valueExpr, allowExplicit: false, apply: true) && inferred is not null)
            Error(line, col, messageTemplate.Replace("{1}", inferred));
    }

    private void Error(TgmlExpression expr, string msg) =>
        _ctx.AddError(msg, _file.FileName, expr.Line, expr.Column);

    private void Error(int line, int col, string msg) =>
        _ctx.AddError(msg, _file.FileName, line, col);

    private void DefineImplicitLocal(string name, TgmlExpression? initializer, int line, int col, string kind)
    {
        if (initializer is null)
        {
            Error(line, col, $"Implicitly typed {kind} '{name}' must have an initializer.");
            Symbols.Define(name, MakeTypeRef("any"));
            return;
        }

        var inferred = InferType(initializer);
        if (inferred is null || inferred == "null")
        {
            Error(line, col, $"Cannot infer the type of implicitly typed {kind} '{name}'.");
            Symbols.Define(name, MakeTypeRef("any"));
            return;
        }

        Symbols.Define(name, MakeTypeRef(inferred));
    }

    private static TgmlTypeRef MakeTypeRef(string name)
    {
        var trimmed = name.Trim();
        var arrayDepth = 0;
        while (trimmed.EndsWith("[]", StringComparison.Ordinal))
        {
            arrayDepth++;
            trimmed = trimmed[..^2];
        }

        var genericStart = trimmed.IndexOf('<');
        if (genericStart < 0)
        {
            return new TgmlTypeRef
            {
                Name = MakeQualifiedName(trimmed),
                ArrayDepth = arrayDepth
            };
        }

        var typeArgsPayload = trimmed[(genericStart + 1)..^1];
        return new TgmlTypeRef
        {
            Name = MakeQualifiedName(trimmed[..genericStart]),
            TypeArgs = SplitTopLevelTypeArgs(typeArgsPayload).Select(MakeTypeRef).ToList(),
            ArrayDepth = arrayDepth
        };
    }

    private static TgmlQualifiedName MakeQualifiedName(string name)
    {
        return new TgmlQualifiedName
        {
            Parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }

    private static List<string> SplitTopLevelTypeArgs(string payload)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < payload.Length; i++)
        {
            switch (payload[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(payload[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(payload[start..].Trim());
        return parts;
    }

    public void CheckBaseConstructorCall(TgmlConstructorDecl ctor)
    {
        foreach (var arg in ctor.BaseArgs ?? [])
            CheckExpr(arg.Value);

        if (_owner is not TgmlClassDecl ownerClass || ctor.BaseArgs is null)
            return;

        var baseClass = ResolveBaseClass(ownerClass);
        if (baseClass is null || baseClass.Constructors.Count == 0)
            return;

        if (!CallArgumentBinder.TryResolveOverload(
                baseClass.Constructors,
                c => c.Params,
                ctor.BaseArgs,
                InferType,
                CanAssignImplicitly,
                out var resolvedCtor,
                out var bound,
                out var error))
        {
            var line = ctor.BaseArgs.FirstOrDefault()?.Value.Line ?? 0;
            var column = ctor.BaseArgs.FirstOrDefault()?.Value.Column ?? 0;
            Error(line, column, $"No base constructor overload matches the supplied arguments: {error}");
            return;
        }

        ctor.Metadata["NormalizedBaseArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        ctor.Metadata["ResolvedBaseConstructor"] = resolvedCtor!;
    }

    private string? InferLValueType(TgmlExpression target)
    {
        return target switch
        {
            TgmlIdExpr id => InferLValueIdType(id),
            TgmlFieldAccessExpr fieldAccess => InferLValueFieldAccessType(fieldAccess),
            TgmlIndexExpr indexExpr => VisitIndex(indexExpr),
            _ => InferType(target)
        };
    }

    private string? InferLValueIdType(TgmlIdExpr id)
    {
        if (Symbols.TryResolve(id.Name, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);

        var prop = ResolvePropertyOnCurrentType(id.Name);
        if (prop is not null)
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);

        var field = FindFieldInHierarchy(_owner, id.Name);
        return field is null ? null : DefaultExpressionFacts.DescribeType(field.Type);
    }

    private string? InferLValueFieldAccessType(TgmlFieldAccessExpr expr)
    {
        if (TryResolveStaticAssetMemberAccess(expr, out var assetName, out var assetType))
        {
            expr.Metadata[AssetReferenceNameMetadata] = assetName;
            return assetType;
        }

        var targetType = InferType(expr.Target);
        if (targetType is null) return null;

        if (!_ctx.TypeTable.TryResolve(targetType, out var targetDecl) || targetDecl is null)
            return null;

        var prop = PropertyAccessHelper.FindPropertyInHierarchy(_ctx.TypeTable, targetDecl, expr.FieldName);
        if (prop is not null)
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);

        var field = FindFieldInHierarchy(targetDecl, expr.FieldName);
        return field is null ? null : DefaultExpressionFacts.DescribeType(field.Type);
    }

    private bool TryResolveAssetAccess(TgmlExpression target, out string assetName, out string assetType)
    {
        switch (target)
        {
            case TgmlIdExpr id when TryResolveCurrentTypeAssetMember(id.Name, out assetName, out assetType):
                id.Metadata[AssetReferenceNameMetadata] = assetName;
                return true;

            case TgmlFieldAccessExpr fieldAccess when TryResolveStaticAssetMemberAccess(fieldAccess, out assetName, out assetType):
                fieldAccess.Metadata[AssetReferenceNameMetadata] = assetName;
                return true;

            default:
                assetName = string.Empty;
                assetType = string.Empty;
                return false;
        }
    }

    private bool TryResolveCurrentTypeAssetMember(string memberName, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;
        if (_owner is null)
            return false;

        return TryResolveAssetMember(_owner, memberName, staticOnly: true, out assetName, out assetType);
    }

    private bool TryResolveStaticAssetMemberAccess(TgmlFieldAccessExpr access, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;

        if (access.Target is not TgmlIdExpr id)
            return false;

        if (Symbols.TryResolve(id.Name, out var symbolType) && symbolType is not null)
            return false;

        if (!_ctx.TypeTable.TryResolve(id.Name, out var targetDecl) || targetDecl is null)
            return false;

        return TryResolveAssetMember(targetDecl, access.FieldName, staticOnly: true, out assetName, out assetType);
    }

    private static bool TryResolveAssetMember(TgmlTypeDecl owner, string memberName, bool staticOnly, out string assetName, out string assetType)
    {
        assetName = string.Empty;
        assetType = string.Empty;

        switch (owner)
        {
            case TgmlClassDecl cls:
            {
                var property = cls.Properties.FirstOrDefault(p =>
                    (!staticOnly || p.IsStatic) &&
                    string.Equals(p.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(p, out _));
                if (property is not null && AssetFacts.TryGetAssetName(property, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(property.Type);
                    return true;
                }

                var field = cls.Fields.FirstOrDefault(f =>
                    (!staticOnly || f.IsStatic) &&
                    string.Equals(f.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(f, out _));
                if (field is not null && AssetFacts.TryGetAssetName(field, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(field.Type);
                    return true;
                }

                break;
            }

            case TgmlStructDecl str:
            {
                var property = str.Properties.FirstOrDefault(p =>
                    (!staticOnly || p.IsStatic) &&
                    string.Equals(p.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(p, out _));
                if (property is not null && AssetFacts.TryGetAssetName(property, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(property.Type);
                    return true;
                }

                var field = str.Fields.FirstOrDefault(f =>
                    (!staticOnly || f.IsStatic) &&
                    string.Equals(f.Name, memberName, StringComparison.Ordinal) &&
                    AssetFacts.TryGetAssetName(f, out _));
                if (field is not null && AssetFacts.TryGetAssetName(field, out assetName))
                {
                    assetType = DefaultExpressionFacts.DescribeType(field.Type);
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private static bool TryGetArrayElementType(string describedType, out string elementType)
    {
        if (describedType.EndsWith("[]", StringComparison.Ordinal))
        {
            elementType = describedType[..^2];
            return true;
        }

        elementType = string.Empty;
        return false;
    }

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

    private TgmlFieldDecl? FindFieldInHierarchy(TgmlTypeDecl? type, string name)
    {
        return FindFieldInHierarchy(type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    private TgmlClassDecl? ResolveBaseClass(TgmlClassDecl ownerClass)
    {
        foreach (var baseRef in ownerClass.BaseTypes)
        {
            if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is TgmlClassDecl baseClass)
                return baseClass;
        }

        return null;
    }

    private List<TgmlMethodDecl> FindMethodsInHierarchy(TgmlTypeDecl? type, string name)
    {
        return FindMethodsInHierarchy(type, name, new HashSet<string>(StringComparer.Ordinal));
    }

    private List<TgmlMethodDecl> FindMethodsInHierarchy(TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return [];

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return [];

        var methods = type switch
        {
            TgmlClassDecl cls => cls.Methods.Where(m => m.Name == name).ToList(),
            TgmlStructDecl str => str.Methods.Where(m => m.Name == name).ToList(),
            TgmlInterfaceDecl iface => iface.Methods
                .Where(m => m.Name == name)
                .Select(m => new TgmlMethodDecl
                {
                    Name = m.Name,
                    Access = AccessModifier.Public,
                    Modifiers = new MethodModifiers(AccessModifier.Public, false, VirtualModifier.None),
                    ReturnType = m.ReturnType,
                    Params = m.Params,
                    TypeParams = m.TypeParams,
                    Decorators = m.Decorators,
                    Body = m.Body
                }).ToList(),
            _ => []
        };

        if (methods.Count > 0)
            return methods;

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };

        if (baseRefs is null) return [];

        foreach (var baseRef in baseRefs)
        {
            if (!_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindMethodsInHierarchy(baseDecl, name, visited);
            if (found.Count > 0)
                return found;
        }

        return [];
    }

    private TgmlFieldDecl? FindFieldInHierarchy(TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return null;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return null;

        var own = type switch
        {
            TgmlClassDecl cls => cls.Fields.FirstOrDefault(f => f.Name == name),
            TgmlStructDecl str => str.Fields.FirstOrDefault(f => f.Name == name),
            _ => null
        };
        if (own is not null) return own;

        var baseRefs = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (baseRefs is null) return null;

        foreach (var baseRef in baseRefs)
        {
            if (!_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null)
                continue;

            var found = FindFieldInHierarchy(baseDecl, name, visited);
            if (found is not null) return found;
        }

        return null;
    }
}

