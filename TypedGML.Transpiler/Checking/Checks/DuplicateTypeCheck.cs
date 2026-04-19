using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>Batch 1: Detects duplicate type registrations across all files.</summary>
public sealed class DuplicateTypeCheck : IAtomicCheck
{
    public string Name => "DuplicateTypeCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            var qn = $"{type.QualifiedName ?? type.Name}`{type.TypeParams.Count}";
            if (seen.TryGetValue(qn, out var firstFile))
            {
                context.AddError(
                    $"Duplicate type '{type.QualifiedName ?? type.Name}' (also defined in '{firstFile}').",
                    file.FileName);
            }
            else
            {
                seen[qn] = file.FileName;
            }
        }
    }
}
