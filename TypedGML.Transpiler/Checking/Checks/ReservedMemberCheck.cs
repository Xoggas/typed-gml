using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed class ReservedMemberCheck : IAtomicCheck
{
    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckType(context, file, type);
    }

    private static void CheckType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                CheckReservedMethods(ctx, file, cls, cls.Methods);
                foreach (var nested in cls.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
            case TgmlStructDecl str:
                CheckReservedMethods(ctx, file, str, str.Methods);
                foreach (var nested in str.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
        }
    }

    private static void CheckReservedMethods(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeDecl owner,
        IEnumerable<TgmlMethodDecl> methods)
    {
        foreach (var method in methods)
        {
            if (string.Equals(method.Name, ObjectFacts.GetTypeMethodName, StringComparison.Ordinal))
            {
                var ownerName = owner.QualifiedName ?? owner.Name;
                ctx.AddError($"'{ownerName}.{ObjectFacts.GetTypeMethodName}' is compiler-reserved and cannot be declared explicitly.", file.FileName);
            }
        }
    }
}
