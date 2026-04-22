using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
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
            TgmlNewImplicitExpr e => EmitNewImplicit(e),
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

    private string GmlTypeName(TgmlTypeRef typeRef)
    {
        if (_ctx.TypeTable.TryResolve(typeRef.Name.Full, out var decl) && decl?.QualifiedName is { } qn)
            return qn.Replace(".", "_");

        return typeRef.GmlBaseName;
    }

    private string EmitFieldKeyword()
    {
        if (_ctx.CurrentPropertyName is { } currentPropertyName &&
            _ctx.GetNativePropertyName(currentPropertyName) is { } nativePropertyName)
        {
            return nativePropertyName;
        }

        return _ctx.CurrentPropertyName is { } propertyName
            ? $"__backing_{propertyName}"
            : "__backing_field";
    }

    private string EmitDefault(TgmlDefaultExpr expr)
    {
        var typeName = DefaultExpressionFacts.GetEffectiveTypeName(expr);
        if (typeName is null || typeName.EndsWith("[]", StringComparison.Ordinal))
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
            return $"new {gmlName}({string.Join(", ", explicitType.TypeArgs.Select(EmitTypeRefAsRuntimeId))})";

        return $"new {gmlName}()";
    }
}
