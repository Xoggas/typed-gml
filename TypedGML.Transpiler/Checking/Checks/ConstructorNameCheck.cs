using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 1: Detects methods whose name matches the enclosing type's name — the classic
///     mistake of writing <c>public Player(...) { }</c> instead of
///     <c>public constructor(...) { }</c>.
///     Also catches empty member names produced by ANTLR error-recovery, converting them
///     into readable diagnostics instead of letting downstream code crash.
/// </summary>
public sealed class ConstructorNameCheck : IAtomicCheck
{
    public string Name => "ConstructorNameCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        {
            foreach (var decl in file.TypeDecls)
            {
                CheckDecl(context, file, decl);
            }
        }
    }

    private static void CheckDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        var methods = decl switch
        {
            TgmlClassDecl cls => cls.Methods,
            TgmlStructDecl str => str.Methods,
            _ => null
        };

        if (methods is null)
        {
            return;
        }

        foreach (var method in methods)
        {
            // Empty name = ANTLR error-recovery placeholder (e.g. method name was missing)
            if (string.IsNullOrEmpty(method.Name))
            {
                ctx.AddError(
                    $"A member in '{decl.Name}' could not be parsed — check for missing keywords or typos.",
                    file.FileName);
                continue;
            }

            // Method name matches the enclosing type name → likely a mislabeled constructor
            if (method.Name == decl.Name)
            {
                ctx.AddError(
                    $"'{decl.Name}' has a method named '{method.Name}' which is the same as the type name. " +
                    $"Did you mean to write 'constructor' instead of specifying a return type? " +
                    $"Example: 'public constructor({string.Join(", ", method.Params.Select(p => $"{p.Type} {p.Name}"))}) {{ ... }}'",
                    file.FileName);
            }
        }

        // Recurse into nested types
        var nestedTypes = decl switch
        {
            TgmlClassDecl cls => cls.NestedTypes,
            TgmlStructDecl str => str.NestedTypes,
            _ => null
        };

        if (nestedTypes is not null)
        {
            foreach (var nested in nestedTypes)
            {
                CheckDecl(ctx, file, nested);
            }
        }
    }
}