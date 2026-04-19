using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates property accessor modifier combinations.
/// </summary>
public sealed class AccessorModifierCheck : IAtomicCheck
{
    public string Name => "AccessorModifierCheck";

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
        if (property.Getter?.AccessMod is { } getterAccess &&
            IsMoreAccessible(getterAccess, property.Access))
        {
            ctx.AddError(
                $"Getter of property '{property.Name}' cannot be more accessible than the property itself.",
                file.FileName);
        }

        if (property.Setter?.AccessMod is { } setterAccess &&
            IsMoreAccessible(setterAccess, property.Access))
        {
            ctx.AddError(
                $"Setter of property '{property.Name}' cannot be more accessible than the property itself.",
                file.FileName);
        }
    }

    private static bool IsMoreAccessible(AccessModifier candidate, AccessModifier baseline)
        => AccessibilityRank(candidate) > AccessibilityRank(baseline);

    private static int AccessibilityRank(AccessModifier access)
        => access switch
        {
            AccessModifier.Private => 0,
            AccessModifier.Protected => 1,
            AccessModifier.Public => 2,
            _ => 0
        };
}
