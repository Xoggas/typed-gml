using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 2: Validates that <c>override</c> methods and properties have a corresponding
///     <c>virtual</c> or <c>abstract</c> member in the inheritance chain, and that
///     abstract members only appear on abstract classes.
/// </summary>
public sealed class OverrideCorrectnessCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            if (type is TgmlClassDecl cls) CheckClass(context, file, cls);
            else if (type is TgmlStructDecl str) CheckStruct(context, file, str);
        }
    }

    // -- Class checks ----------------------------------------------------------

    private static void CheckClass(TranspileContext ctx, TgmlFile file, TgmlClassDecl cls)
    {
        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract && !cls.IsAbstract)
                ctx.AddError(
                    $"Non-abstract class '{cls.Name}' cannot declare abstract method '{method.Name}'. " +
                    $"Mark the class 'abstract' or provide an implementation.",
                    file.FileName);

            if (method.IsOverride && !FindOverridableMethod(ctx, cls, method.Name))
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

            if (prop.Modifiers.Virtual == VirtualModifier.Override &&
                !FindOverridableProp(ctx, cls, prop.Name))
                ctx.AddError(
                    $"'{cls.Name}.{prop.Name}' is marked 'override' but no virtual or abstract property " +
                    $"with this name was found in the base type chain.",
                    file.FileName);
        }
    }

    private static void CheckStruct(TranspileContext ctx, TgmlFile file, TgmlStructDecl str)
    {
        foreach (var method in str.Methods)
            if (method.IsOverride && !FindOverridableMethod(ctx, str, method.Name))
                ctx.AddError(
                    $"'{str.Name}.{method.Name}' is marked 'override' but no matching virtual/abstract " +
                    $"method was found in the base type chain.",
                    file.FileName);
    }

    private static bool FindOverridableMethod(TranspileContext ctx, TgmlTypeDecl decl, string name)
        => FindOverridableMethod(ctx, decl, name, new HashSet<string>(StringComparer.Ordinal));

    private static bool FindOverridableMethod(
        TranspileContext ctx,
        TgmlTypeDecl decl,
        string name,
        HashSet<string> visited)
    {
        foreach (var baseDecl in EnumerateBaseDecls(ctx, decl))
        {
            var key = baseDecl.QualifiedName ?? baseDecl.Name;
            if (!visited.Add(key))
                continue;

            if (baseDecl is TgmlClassDecl baseClass &&
                baseClass.Methods.Any(m => m.Name == name &&
                                           m.Modifiers.Virtual is VirtualModifier.Virtual
                                               or VirtualModifier.Abstract or VirtualModifier.Override))
            {
                return true;
            }

            if (FindOverridableMethod(ctx, baseDecl, name, visited))
                return true;
        }

        return false;
    }

    private static bool FindOverridableProp(TranspileContext ctx, TgmlTypeDecl decl, string name)
        => FindOverridableProp(ctx, decl, name, new HashSet<string>(StringComparer.Ordinal));

    private static bool FindOverridableProp(
        TranspileContext ctx,
        TgmlTypeDecl decl,
        string name,
        HashSet<string> visited)
    {
        foreach (var baseDecl in EnumerateBaseDecls(ctx, decl))
        {
            var key = baseDecl.QualifiedName ?? baseDecl.Name;
            if (!visited.Add(key))
                continue;

            if (baseDecl is TgmlClassDecl baseClass &&
                baseClass.Properties.Any(p => p.Name == name &&
                                              p.Modifiers.Virtual is VirtualModifier.Virtual
                                                  or VirtualModifier.Abstract or VirtualModifier.Override))
            {
                return true;
            }

            if (FindOverridableProp(ctx, baseDecl, name, visited))
                return true;
        }

        return false;
    }

    private static IEnumerable<TgmlTypeDecl> EnumerateBaseDecls(TranspileContext ctx, TgmlTypeDecl decl)
    {
        var baseRefs = decl switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };

        if (baseRefs is not null)
        {
            foreach (var baseRef in baseRefs)
            {
                if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                    yield return baseDecl;
            }
        }

        if (ObjectFacts.TryResolveImplicitObject(ctx.TypeTable, decl, out var systemObject))
            yield return systemObject;
    }
}

