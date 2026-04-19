using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 1: Walks all type declarations and registers them in the TypeTable.
///     Sets the QualifiedName on each TgmlTypeDecl.
/// </summary>
public sealed class TypeRegistrationCheck : IAtomicCheck
{
    public string Name => "TypeRegistration";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        {
            var ns = file.PrimaryNamespace;
            RegisterTypes(context, file.TypeDecls, ns);
        }
    }

    private static void RegisterTypes(TranspileContext ctx, IEnumerable<TgmlTypeDecl> types, string ns)
    {
        foreach (var type in types)
        {
            var qualifiedName = string.IsNullOrEmpty(ns) ? type.Name : $"{ns}.{type.Name}";
            ctx.TypeTable.Register(type, qualifiedName);

            // Register nested types
            var nestedTypes = type switch
            {
                TgmlClassDecl c => c.NestedTypes,
                TgmlStructDecl s => s.NestedTypes,
                _ => null
            };
            if (nestedTypes is not null)
            {
                RegisterTypes(ctx, nestedTypes, qualifiedName);
            }
        }
    }
}
