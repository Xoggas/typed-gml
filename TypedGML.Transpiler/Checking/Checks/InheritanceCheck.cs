using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>Batch 2: Verifies that base types and interfaces referenced in inheritance lists exist. Unknown base types are errors.</summary>
public sealed class InheritanceCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            CheckType(context, file, type);
        }
    }

    private static void CheckType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        // Enums and delegates do not support inheritance at all
        if (decl is TgmlEnumDecl or TgmlDelegateDecl)
            return;

        var bases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            TgmlInterfaceDecl i => i.BaseInterfaces,
            _ => null
        };

        if (bases is null || bases.Count == 0)
            return;

        // Static classes cannot inherit from anything
        if (decl is TgmlClassDecl { ClassModifier: ClassModifier.Static })
        {
            ctx.AddError(
                $"Static class '{decl.Name}' cannot have a base type.",
                file.FileName);
            return;
        }

        foreach (var baseRef in bases)
        {
            var name = baseRef.Name.Full;
            if (!ctx.TypeTable.TryResolve(name, out var baseDecl) && !IsBuiltIn(name))
            {
                ctx.AddError(
                    $"Type '{decl.Name}' references unknown base type '{name}'.",
                    file.FileName);
                continue;
            }

            if (baseDecl is null)
                continue;

            // Cannot inherit from a sealed class
            if (baseDecl is TgmlClassDecl { IsSealed: true })
            {
                ctx.AddError(
                    $"'{decl.Name}' cannot inherit from '{name}' because it is sealed.",
                    file.FileName);
            }

            // Struct cannot inherit from a class (except the implicit/explicit System.Object base)
            if (decl is TgmlStructDecl &&
                baseDecl is TgmlClassDecl &&
                !ObjectFacts.IsSystemObject(baseDecl))
            {
                ctx.AddError(
                    $"Struct '{decl.Name}' cannot inherit from class '{name}'. Structs may only inherit from interfaces.",
                    file.FileName);
            }
            else if (decl is TgmlClassDecl && baseDecl is TgmlStructDecl)
            {
                ctx.AddError(
                    $"Class '{decl.Name}' cannot inherit from struct '{name}'.",
                    file.FileName);
            }

            // Cannot inherit from a static class
            if (baseDecl is TgmlClassDecl { ClassModifier: ClassModifier.Static })
            {
                ctx.AddError(
                    $"'{decl.Name}' cannot inherit from static class '{name}'.",
                    file.FileName);
            }
        }
    }

    private static bool IsBuiltIn(string name)
    {
        return name is "object" or "struct";
    }
}
