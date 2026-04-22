using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 1: Verifies that <c>readonly struct</c> declarations only contain
///     readonly fields and get-only properties (no setter).
///     Also rejects <c>readonly</c> modifier on properties anywhere (only fields can be readonly).
/// </summary>
public sealed class ReadonlyStructCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            CheckReadonlyProperties(context, file, type);
            if (type is TgmlStructDecl { IsReadonly: true } str)
                CheckReadonlyStruct(context, file, str);
        }
    }

    private static void CheckReadonlyProperties(TranspileContext ctx, TgmlFile file, TgmlTypeDecl type)
    {
        var properties = type switch
        {
            TgmlClassDecl c  => c.Properties,
            TgmlStructDecl s => s.Properties,
            _                => null
        };
        if (properties is null) return;

        foreach (var prop in properties)
        {
            if (prop.Modifiers.IsReadonly)
                ctx.AddError(
                    $"Property '{prop.Name}' in '{type.Name}' cannot be declared readonly. Only fields support the readonly modifier.",
                    file.FileName);
        }
    }

    private static void CheckReadonlyStruct(TranspileContext ctx, TgmlFile file, TgmlStructDecl str)
    {
        foreach (var field in str.Fields)
        {
            if (!field.IsReadonly && !field.IsConst)
            {
                ctx.AddError(
                    $"Field '{field.Name}' in readonly struct '{str.Name}' must be declared readonly.",
                    file.FileName);
            }
        }

        foreach (var prop in str.Properties)
        {
            if (prop.Setter is not null)
            {
                ctx.AddError(
                    $"Property '{prop.Name}' in readonly struct '{str.Name}' cannot have a setter.",
                    file.FileName);
            }
        }
    }
}

