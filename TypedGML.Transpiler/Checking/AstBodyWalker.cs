using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public abstract partial class AstBodyWalker : IAtomicCheck
{
    public abstract string Name { get; }

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            WalkType(context, file, type);
    }

    protected virtual void OnEnterType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl) { }
    protected virtual void OnLeaveType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl) { }
    protected virtual void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx) { }
    protected virtual void OnLeaveCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx) { }
    protected virtual void OnEnterBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx) { }
    protected virtual void OnLeaveBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx) { }

    protected virtual bool OnStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
        => true;

    protected virtual void AfterStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx) { }
    protected virtual void OnExpression(TranspileContext ctx, TgmlFile file, TgmlExpression expr, WalkContext wctx) { }
}
