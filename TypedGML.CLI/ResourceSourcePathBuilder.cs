using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Symbols;

namespace TypedGML.CLI;

internal static class ResourceSourcePathBuilder
{
    public static IReadOnlyDictionary<string, string> Build(
        IReadOnlyList<FileNode> files,
        SymbolTable symbols)
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
            AddDeclarations(file.TopLevelDeclarations, string.Empty, file.FilePath, symbols, paths);
        return paths;
    }

    private static void AddDeclarations(
        IEnumerable<IAstNode> nodes,
        string currentNamespace,
        string filePath,
        SymbolTable symbols,
        Dictionary<string, string> paths)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    AddDeclarations(ns.Body, Combine(currentNamespace, ns.Name), filePath, symbols, paths);
                    break;
                case ClassDeclarationNode type:
                    AddType(type.Name, type.GenericParams.Count, currentNamespace, filePath, symbols, paths);
                    break;
                case StructDeclarationNode type:
                    AddType(type.Name, type.GenericParams.Count, currentNamespace, filePath, symbols, paths);
                    break;
                case EnumDeclarationNode type:
                    AddType(type.Name, 0, currentNamespace, filePath, symbols, paths);
                    break;
                case FunctionDeclarationNode function:
                    AddFunction(function.Name, currentNamespace, filePath, paths);
                    break;
            }
        }
    }

    private static void AddType(
        string name,
        int arity,
        string currentNamespace,
        string filePath,
        SymbolTable symbols,
        Dictionary<string, string> paths)
    {
        var type = ResolveType(name, arity, currentNamespace, symbols);
        paths[NamingConvention.TypeName(type)] = filePath;
        if (!string.IsNullOrWhiteSpace(type.ObjectAssetName))
            paths[type.ObjectAssetName] = filePath;
    }

    private static void AddFunction(
        string name,
        string currentNamespace,
        string filePath,
        Dictionary<string, string> paths)
    {
        var organizer = new FileOrganizer(string.Empty);
        paths[Path.GetFileNameWithoutExtension(organizer.GetFunctionPath(name, currentNamespace))] = filePath;
    }

    private static TypeSymbol ResolveType(
        string name,
        int arity,
        string currentNamespace,
        SymbolTable symbols)
    {
        if (symbols.TryResolve(name, arity, currentNamespace, [], out var type))
            return type;

        var fallback = new TypeSymbol { QualifiedName = Combine(currentNamespace, name) };
        for (var index = 0; index < arity; index++)
            fallback.GenericParameters.Add(new GenericParamNode($"T{index}", string.Empty, new SourceLocation(string.Empty, 0, 0)));
        return fallback;
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
