using System.IO;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

public sealed class FileOrganizer(string outputRoot)
{
    public string GetScriptPath(TypeSymbol type) =>
        Path.Combine(outputRoot, "scripts", $"{NamingConvention.TypeName(type)}.gml");

    public string GetEventPath(TypeSymbol type, string eventName)
    {
        var objectName = type.ObjectAssetName ?? NamingConvention.TypeName(type);
        return Path.Combine(outputRoot, "objects", objectName, $"{eventName}.gml");
    }

    public string GetMacroPath(string namespaceName) =>
        Path.Combine(outputRoot, "macros", $"{namespaceName.Replace(".", "_", StringComparison.Ordinal)}_macros.gml");
}
