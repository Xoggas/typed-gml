using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits an enum as a set of #macro definitions.
///     e.g. public enum Color { Red = 0, Green, Blue }
///     → #macro Color_Red 0 / #macro Color_Green 1 / #macro Color_Blue 2
///     Also emits a #macro __TYPE_EnumName N for runtime type checks.
/// </summary>
public sealed class EnumEmitter : ICodeEmitter
{
    public bool CanEmit(TgmlTypeDecl decl)
    {
        return decl is TgmlEnumDecl;
    }

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var enumDecl = (TgmlEnumDecl)decl;
        var w = new GmlWriter();
        var prefix = enumDecl.QualifiedName?.Replace(".", "_") ?? enumDecl.Name;

        long counter = 0;
        foreach (var member in enumDecl.Members)
        {
            if (member.Value is not null)
            {
                counter = member.Value.ParsedValue;
            }

            w.WriteLine($"#macro {prefix}_{member.Name} {counter}");
            counter++;
        }

        var scriptName = prefix;
        yield return new GeneratedFile($"Scripts/{scriptName}/{scriptName}.gml", w.ToString());
    }
}