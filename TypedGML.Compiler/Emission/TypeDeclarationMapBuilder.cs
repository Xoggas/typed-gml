using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;

namespace TypedGML.Compiler.Emission;

internal static class TypeDeclarationMapBuilder
{
    public static IReadOnlyDictionary<string, IAstNode> Build(IReadOnlyList<FileNode> files)
    {
        var map = new Dictionary<string, IAstNode>(StringComparer.Ordinal);
        foreach (var file in files)
            Visit(file.TopLevelDeclarations, string.Empty, map);
        return map;
    }

    private static void Visit(IEnumerable<IAstNode> nodes, string currentNamespace, IDictionary<string, IAstNode> map)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    Visit(ns.Body, Combine(currentNamespace, ns.Name), map);
                    break;
                case ClassDeclarationNode type:
                    map[Combine(currentNamespace, type.Name)] = type;
                    break;
                case StructDeclarationNode type:
                    map[Combine(currentNamespace, type.Name)] = type;
                    break;
                case InterfaceDeclarationNode type:
                    map[Combine(currentNamespace, type.Name)] = type;
                    break;
            }
        }
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
