using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Converts TypedGML expression nodes to GML source strings.
/// </summary>
public sealed class ExpressionEmitter
{
    private readonly GenerationContext _ctx;

    public ExpressionEmitter(GenerationContext ctx)
    {
        _ctx = ctx;
    }

    public string Emit(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlMethodCallExpr e => EmitMethodCall(e),
            TgmlFieldAccessExpr e => EmitFieldAccess(e),
            TgmlIndexExpr e => $"{Emit(e.Target)}[{Emit(e.Index)}]",
            TgmlUnaryExpr e => EmitUnary(e),
            TgmlCastExpr e => Emit(e.Operand), // GML has no casts; strip
            TgmlBinaryExpr e => EmitBinary(e),
            TgmlIsExpr e => EmitIs(e),
            TgmlAsExpr e => EmitAs(e),
            TgmlTernaryExpr e => $"{Emit(e.Condition)} ? {Emit(e.ThenExpr)} : {Emit(e.ElseExpr)}",
            TgmlAssignExpr e => EmitAssign(e),
            TgmlNewObjectExpr e => EmitNewObject(e),
            TgmlNewArrayExpr e => $"array_create({Emit(e.Size)})",
            TgmlTypeofExpr e => $"__TYPE_{GmlTypeName(e.Type)}",
            TgmlNameofExpr e => $"\"{Emit(e.Operand)}\"",
            TgmlBaseCallExpr e => EmitBaseCall(e),
            TgmlBaseAccessExpr e => EmitBaseAccess(e),
            TgmlLambdaExpr e => EmitLambda(e),
            TgmlArrayInitExpr e => $"[{string.Join(", ", e.Elements.Select(Emit))}]",
            TgmlFuncCallExpr e => $"{e.FunctionName}({string.Join(", ", e.Args.Select(Emit))})",
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
        if (_ctx.IsGlobalProperty && _ctx.CurrentPropertyName is { } pn)
        {
            return $"global.{pn}";
        }

        if (_ctx.CurrentPropertyName is { } pn2)
        {
            return $"__backing_{pn2}";
        }

        return "__backing_field";
    }

    private string EmitMethodCall(TgmlMethodCallExpr e)
    {
        var target = Emit(e.Target);
        var args = string.Join(", ", e.Args.Select(Emit));

        // If target is the with-alias, strip it → implicit self inside with block
        if (_ctx.WithAlias is { } alias && target == alias)
        {
            return $"{e.MethodName}({args})";
        }

        return $"{target}.{e.MethodName}({args})";
    }

    private string EmitFieldAccess(TgmlFieldAccessExpr e)
    {
        var target = Emit(e.Target);
        if (_ctx.WithAlias is { } alias && target == alias)
        {
            return e.FieldName;
        }

        return $"{target}.{e.FieldName}";
    }

    // ── Property-aware read / write ───────────────────────────────────────────

    /// <summary>
    ///     Emits an identifier in read context. If the name is a property on the current type
    ///     (and we are not already inside that property's own accessor), the read is redirected
    ///     to the GML getter call <c>get_PropName()</c>.
    /// </summary>
    private string EmitIdRead(TgmlIdExpr e)
    {
        if (!_ctx.IsInsideAccessorOf(e.Name))
        {
            var prop = _ctx.FindProperty(e.Name);
            if (prop?.Getter is not null)
            {
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
        // Don't redirect if we're already inside this property's own accessor
        if (!IsWriteTargetInOwnAccessor(e.Target))
        {
            var (propName, qualifier) = ExtractPropertyAccess(e.Target);
            if (propName is not null && _ctx.FindProperty(propName)?.Setter is not null)
            {
                var setPrefix = qualifier is not null ? $"{qualifier}." : string.Empty;
                var setterCall = $"{setPrefix}set_{propName}";

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
            TgmlIdExpr e => e.Name,
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
    private (string? propName, string? qualifier) ExtractPropertyAccess(TgmlExpression target)
    {
        // Bare name: Name = ...
        if (target is TgmlIdExpr id && _ctx.FindProperty(id.Name) is not null)
        {
            return (id.Name, null);
        }

        // self.Name = ...  or  this.Name = ...
        if (target is TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa
            && _ctx.FindProperty(fa.FieldName) is not null)
        {
            return (fa.FieldName, null);
        }

        return (null, null);
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

    private string EmitUnary(TgmlUnaryExpr e)
    {
        var op = e.Operator switch
        {
            "not" => "!",
            _ => e.Operator
        };
        return $"{op}{Emit(e.Operand)}";
    }

    private string EmitBinary(TgmlBinaryExpr e)
    {
        var op = e.Operator switch
        {
            "and" => "&&",
            "or" => "||",
            _ => e.Operator
        };
        return $"{Emit(e.Left)} {op} {Emit(e.Right)}";
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
            var args = e.Args.Select(Emit).ToList();

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
        var userArgs = e.Args.Select(Emit).ToList();
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
        // In @Object context → event_inherited()
        if (_ctx.CurrentType is TgmlClassDecl cls && cls.IsGameObject)
        {
            return "event_inherited()";
        }

        // In script class → try to inline parent method (best-effort: call parent_Method)
        var parentMethod = e.MethodName;
        var args = string.Join(", ", e.Args.Select(Emit));
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
            return $"function({paramStr}) {w}";
        }

        return $"function({paramStr}) {{}}";
    }
}