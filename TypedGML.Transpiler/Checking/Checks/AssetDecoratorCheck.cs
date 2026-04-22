using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates compile-time asset references declared through <c>@Asset("name")</c>.
/// </summary>
public sealed class AssetDecoratorCheck : IAtomicCheck
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
                foreach (var field in cls.Fields)
                    CheckField(ctx, file, field);
                foreach (var prop in cls.Properties)
                    CheckProperty(ctx, file, prop);
                foreach (var nested in cls.NestedTypes)
                    CheckType(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                foreach (var field in str.Fields)
                    CheckField(ctx, file, field);
                foreach (var prop in str.Properties)
                    CheckProperty(ctx, file, prop);
                foreach (var nested in str.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
        }
    }

    private static void CheckField(TranspileContext ctx, TgmlFile file, TgmlFieldDecl field)
    {
        if (!field.HasDecorator(AssetFacts.DecoratorName))
            return;

        if (!field.IsStatic)
        {
            ctx.AddError($"@Asset member '{field.Name}' must be static.", file.FileName);
        }

        if (!AssetFacts.IsAssetType(field.Type))
        {
            ctx.AddError(
                $"@Asset member '{field.Name}' must use one of the asset types: Sprite, Room, Audio, or Font.",
                file.FileName);
        }
    }

    private static void CheckProperty(TranspileContext ctx, TgmlFile file, TgmlPropertyDecl property)
    {
        if (!property.HasDecorator(AssetFacts.DecoratorName))
            return;

        if (!property.IsStatic)
        {
            ctx.AddError($"@Asset member '{property.Name}' must be static.", file.FileName);
        }

        if (property.IsIndexer)
        {
            ctx.AddError("@Asset cannot be applied to an indexer.", file.FileName);
        }

        if (!AssetFacts.IsAssetType(property.Type))
        {
            ctx.AddError(
                $"@Asset member '{property.Name}' must use one of the asset types: Sprite, Room, Audio, or Font.",
                file.FileName);
        }

        if (property.Getter is { IsAuto: false })
        {
            ctx.AddError(
                $"@Asset member '{property.Name}' cannot declare a custom getter body. Use an auto getter instead.",
                file.FileName);
        }

        if (property.Setter is { IsAuto: false })
        {
            ctx.AddError(
                $"@Asset member '{property.Name}' cannot declare a custom setter body. Use an auto setter instead.",
                file.FileName);
        }
    }
}
