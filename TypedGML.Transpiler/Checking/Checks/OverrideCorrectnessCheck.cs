using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 2: Validates that <c>override</c> methods and properties have a corresponding
///     <c>virtual</c> or <c>abstract</c> member in the inheritance chain, and that
///     abstract members only appear on abstract classes.
/// </summary>
public sealed class OverrideCorrectnessCheck : IAtomicCheck
{
    public string Name => "OverrideCorrectnessCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            if (type is TgmlClassDecl cls) CheckClass(context, file, cls);
            else if (type is TgmlStructDecl str) CheckStruct(context, file, str);
        }
    }

    // ── Class checks ──────────────────────────────────────────────────────────

    private static void CheckClass(TranspileContext ctx, TgmlFile file, TgmlClassDecl cls)
    {
        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract && !cls.IsAbstract)
                ctx.AddError(
                    $"Non-abstract class '{cls.Name}' cannot declare abstract method '{method.Name}'. " +
                    $"Mark the class 'abstract' or provide an implementation.",
                    file.FileName);

            if (method.IsOverride && !FindOverridableMethod(ctx, cls.BaseTypes, method.Name))
                ctx.AddError(
                    $"'{cls.Name}.{method.Name}' is marked 'override' but no virtual or abstract method " +
                    $"with this name was found in the base type chain.",
                    file.FileName);
        }

        foreach (var prop in cls.Properties)
        {
            if (prop.Modifiers.Virtual == VirtualModifier.Abstract && !cls.IsAbstract)
                ctx.AddError(
                    $"Non-abstract class '{cls.Name}' cannot declare abstract property '{prop.Name}'.",
                    file.FileName);

            if (prop.Modifiers.Virtual == VirtualModifier.Override
                && !FindOverridableProp(ctx, cls.BaseTypes, prop.Name))
                ctx.AddError(
                    $"'{cls.Name}.{prop.Name}' is marked 'override' but no virtual or abstract property " +
                    $"with this name was found in the base type chain.",
                    file.FileName);
        }
    }

    private static void CheckStruct(TranspileContext ctx, TgmlFile file, TgmlStructDecl str)
    {
        foreach (var method in str.Methods)
            if (method.IsOverride && !FindOverridableMethod(ctx, str.BaseTypes, method.Name))
                ctx.AddError(
                    $"'{str.Name}.{method.Name}' is marked 'override' but no matching virtual/abstract " +
                    $"method was found in the base type chain.",
                    file.FileName);
    }

    // ── Inheritance-chain searches ────────────────────────────────────────────

    private static bool FindOverridableMethod(TranspileContext ctx,
        IEnumerable<TgmlTypeRef> baseRefs, string name)
    {
        foreach (var baseRef in baseRefs)
        {
            if (!ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null) continue;
            if (baseDecl is TgmlClassDecl baseCls)
            {
                if (baseCls.Methods.Any(m => m.Name == name
                                             && m.Modifiers.Virtual is VirtualModifier.Virtual
                                                 or VirtualModifier.Abstract or VirtualModifier.Override))
                    return true;
                if (FindOverridableMethod(ctx, baseCls.BaseTypes, name)) return true;
            }
        }

        return false;
    }

    private static bool FindOverridableProp(TranspileContext ctx,
        IEnumerable<TgmlTypeRef> baseRefs, string name)
    {
        foreach (var baseRef in baseRefs)
        {
            if (!ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null) continue;
            if (baseDecl is TgmlClassDecl baseCls)
            {
                if (baseCls.Properties.Any(p => p.Name == name
                                                && p.Modifiers.Virtual is VirtualModifier.Virtual
                                                    or VirtualModifier.Abstract or VirtualModifier.Override))
                    return true;
                if (FindOverridableProp(ctx, baseCls.BaseTypes, name)) return true;
            }
        }

        return false;
    }
}

