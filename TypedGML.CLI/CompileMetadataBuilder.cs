using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Symbols;

namespace TypedGML.CLI;

internal static class CompileMetadataBuilder
{
    public static IReadOnlyDictionary<string, string> Build(
        IReadOnlyList<FileNode> files,
        SymbolTable symbols)
    {
        var namespaces = new Dictionary<string, string>(StringComparer.Ordinal);
        AddTypeResources(symbols, namespaces);
        foreach (var file in files)
            AddFunctionResources(file.TopLevelDeclarations, null, namespaces);
        return namespaces;
    }

    private static void AddTypeResources(
        SymbolTable symbols,
        Dictionary<string, string> namespaces)
    {
        foreach (var type in symbols.AllTypes)
        {
            if (string.IsNullOrWhiteSpace(type.QualifiedName))
                continue;

            var namespaceName = NamespaceOf(type.QualifiedName);
            namespaces[NamingConvention.TypeName(type)] = namespaceName;
            if (!string.IsNullOrWhiteSpace(type.ObjectAssetName))
                namespaces[type.ObjectAssetName] = namespaceName;
        }
    }

    private static void AddFunctionResources(
        IEnumerable<IAstNode> nodes,
        string? currentNamespace,
        Dictionary<string, string> namespaces)
    {
        foreach (var node in nodes)
        {
            if (node is NamespaceDeclarationNode ns)
            {
                AddFunctionResources(ns.Body, Combine(currentNamespace, ns.Name), namespaces);
                continue;
            }

            if (node is FunctionDeclarationNode function)
                namespaces[FunctionResourceName(function.Name, currentNamespace)] = currentNamespace ?? string.Empty;
        }
    }

    private static string FunctionResourceName(string name, string? currentNamespace)
    {
        var organizer = new FileOrganizer(string.Empty);
        return Path.GetFileNameWithoutExtension(organizer.GetFunctionPath(name, currentNamespace));
    }

    private static string NamespaceOf(string qualifiedName)
    {
        var index = qualifiedName.LastIndexOf('.');
        return index < 0 ? string.Empty : qualifiedName[..index];
    }

    private static string Combine(string? currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
