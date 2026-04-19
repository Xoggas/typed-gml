using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Ensures @NativeProperty declarations stay as direct bindings to native GML variables.
///     Accessor-level access modifiers are allowed, but custom getter/setter bodies are not.
/// </summary>
public sealed class NativePropertyBehaviorCheck : IAtomicCheck
{
    public string Name => "NativePropertyBehaviorCheck";

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
                foreach (var prop in cls.Properties)
                    CheckProperty(ctx, file, prop);
                foreach (var nested in cls.NestedTypes)
                    CheckType(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                foreach (var prop in str.Properties)
                    CheckProperty(ctx, file, prop);
                foreach (var nested in str.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
        }
    }

    private static void CheckProperty(TranspileContext ctx, TgmlFile file, TgmlPropertyDecl property)
    {
        if (!property.HasDecorator("NativeProperty"))
            return;

        if (property.Getter is { IsAuto: false })
        {
            ctx.AddError(
                $"@NativeProperty '{property.Name}' cannot declare a custom getter body. Use an auto getter instead.",
                file.FileName);
        }

        if (property.Setter is { IsAuto: false })
        {
            ctx.AddError(
                $"@NativeProperty '{property.Name}' cannot declare a custom setter body. Use an auto setter instead.",
                file.FileName);
        }
    }
}
