using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class IndexerEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is IndexerDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var indexer = (IndexerDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var symbol = ctx.CurrentType.Members.FirstOrDefault(m =>
            m.Kind == MemberKind.Indexer &&
            m.ReturnType == indexer.TypeRef &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].TypeRef == indexer.Parameter.TypeRef);
        if (symbol is null)
            return;

        foreach (var accessor in indexer.Accessors)
            EmitAccessor(indexer, accessor, symbol, ctx);
    }

    private static void EmitAccessor(
        IndexerDeclarationNode indexer,
        AccessorNode accessor,
        MemberSymbol symbol,
        EmitContext ctx)
    {
        var selfName = ctx.CurrentType?.ObjectAssetName is null ? "self" : null;
        var name = accessor.Kind == AccessorKind.Get
            ? NamingConvention.IndexerGetter(ctx.CurrentType!)
            : NamingConvention.IndexerSetter(ctx.CurrentType!);
        ctx.Writer.Write($"function {name}({ParameterList(indexer, accessor, selfName)})");
        ctx.ResetTempVars();
        if (accessor.Body is null)
        {
            ctx.Writer.BeginBlock();
            ctx.Writer.EndBlock();
            return;
        }

        WithAccessorContext(indexer, accessor, symbol, selfName, ctx, () => ctx.Dispatch(accessor.Body, ctx));
    }

    private static string ParameterList(IndexerDeclarationNode indexer, AccessorNode accessor, string? selfName)
    {
        var parameters = string.IsNullOrEmpty(selfName) ? [] : new[] { selfName };
        return string.Join(", ", accessor.Kind == AccessorKind.Get
            ? [.. parameters, indexer.Parameter.Name]
            : [.. parameters, indexer.Parameter.Name, "value"]);
    }

    private static void WithAccessorContext(
        IndexerDeclarationNode indexer,
        AccessorNode accessor,
        MemberSymbol symbol,
        string? selfName,
        EmitContext ctx,
        Action action)
    {
        var previousMember = ctx.CurrentMember;
        var previousSelf = ctx.SelfName;
        ctx.CurrentMember = symbol;
        ctx.SelfName = selfName;
        ctx.Scope.Push();
        ctx.Scope.Declare(indexer.Parameter.Name, indexer.Parameter.TypeRef);
        if (accessor.Kind == AccessorKind.Set)
            ctx.Scope.Declare("value", indexer.TypeRef);
        try
        {
            action();
        }
        finally
        {
            ctx.Scope.Pop();
            ctx.SelfName = previousSelf;
            ctx.CurrentMember = previousMember;
        }
    }
}
