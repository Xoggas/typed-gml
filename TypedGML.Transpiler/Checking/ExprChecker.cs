using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
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
    private readonly string _returnType;
    private readonly bool _isStaticContext;

    public ExprChecker(
        TranspileContext ctx,
        TgmlFile file,
        SymbolTable symbols,
        TgmlTypeDecl? owner = null,
        string returnType = "void",
        bool isStaticContext = false)
    {
        _ctx = ctx;
        _file = file;
        Symbols = symbols;
        _owner = owner;
        _returnType = returnType;
        _isStaticContext = isStaticContext;
    }

    public SymbolTable Symbols { get; }

    public void CheckExpr(TgmlExpression expr) => InferType(expr);

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
            TgmlNewImplicitExpr ni => VisitNewImplicit(ni),
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
}
