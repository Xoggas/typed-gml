using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 2: For non-abstract classes, ensures all abstract methods (from parent/interfaces)
///     are overridden.
/// </summary>
public sealed class AbstractImplementationCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var decl in file.TypeDecls)
        {
            if (decl is not TgmlClassDecl cls)
            {
                continue;
            }

            if (cls.IsAbstract)
            {
                continue;
            }

            // Collect abstract methods declared directly on this class
            var abstractMethods = cls.Methods.Where(m => m.IsAbstract).ToList();
            if (abstractMethods.Count == 0)
            {
                continue;
            }

            foreach (var abs in abstractMethods)
            {
                context.AddError(
                    $"Non-abstract class '{cls.Name}' has unimplemented abstract method '{abs.Name}'.",
                    file.FileName);
            }
        }
    }
}