using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class GenericConstraintCheck : IAtomicCheck
{
    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckTypeDecl(context, file, type);
    }

    private static void CheckMethod(
        TranspileContext ctx,
        TgmlFile file,
        TgmlMethodDecl method,
        List<TgmlTypeParam> ownerTypeParams)
    {
        var visible = ownerTypeParams.Concat(method.TypeParams).ToList();
        CheckTypeRef(ctx, file, method.ReturnType, visible);
        foreach (var param in method.Params)
            CheckTypeRef(ctx, file, param.Type, visible);
    }
}
