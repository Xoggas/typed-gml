using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>A single atomic semantic check that operates on all parsed files.</summary>
public interface IAtomicCheck
{
    string Name { get; }
    void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files);
}