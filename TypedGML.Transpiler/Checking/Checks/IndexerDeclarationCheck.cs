using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates basic indexer declaration rules.
/// </summary>
public sealed class IndexerDeclarationCheck : IAtomicCheck
{
    public string Name => "IndexerDeclarationCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckType(context, file, type);
    }

    private static void CheckType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl type)
    {
        switch (type)
        {
            case TgmlClassDecl cls:
                foreach (var property in cls.Properties.Where(p => p.IsIndexer))
                    CheckIndexer(ctx, file, property);
                foreach (var nested in cls.NestedTypes)
                    CheckType(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                foreach (var property in str.Properties.Where(p => p.IsIndexer))
                    CheckIndexer(ctx, file, property);
                foreach (var nested in str.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
        }
    }

    private static void CheckIndexer(TranspileContext ctx, TgmlFile file, TgmlPropertyDecl indexer)
    {
        if (!string.Equals(indexer.Name, "this", StringComparison.Ordinal))
        {
            ctx.AddError(
                "Indexers must be declared with the name 'this'.",
                file.FileName);
        }

        if (indexer.IsStatic)
        {
            ctx.AddError(
                "Indexers cannot be static.",
                file.FileName);
        }

        if (indexer.IndexParam?.Default is not null)
        {
            ctx.AddError(
                "Indexer parameters cannot have default values.",
                file.FileName);
        }

        if (indexer.Accessors.Any(a => a.IsAuto))
        {
            ctx.AddError(
                "Indexers cannot be auto-implemented. Declare explicit getter/setter bodies.",
                file.FileName);
        }

        if (indexer.HasDecorator("NativeProperty"))
        {
            ctx.AddError(
                "@NativeProperty is not supported on indexers.",
                file.FileName);
        }
    }
}
