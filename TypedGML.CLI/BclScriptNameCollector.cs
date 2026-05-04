using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Symbols;

namespace TypedGML.CLI;

internal static class BclScriptNameCollector
{
    public static IReadOnlySet<string> Collect(IReadOnlyList<FileNode> files, string bclPath)
    {
        var scriptNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files.Where(file => IsBclFile(file.FilePath, bclPath)))
            AddDeclarations(file.TopLevelDeclarations, string.Empty, scriptNames);

        return scriptNames;
    }

    private static void AddDeclarations(
        IEnumerable<IAstNode> nodes,
        string currentNamespace,
        HashSet<string> scriptNames)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    AddDeclarations(ns.Body, Combine(currentNamespace, ns.Name), scriptNames);
                    break;
                case ClassDeclarationNode type:
                    AddType(type.Name, type.GenericParams.Count, currentNamespace, scriptNames);
                    break;
                case StructDeclarationNode type:
                    AddType(type.Name, type.GenericParams.Count, currentNamespace, scriptNames);
                    break;
                case EnumDeclarationNode type:
                    AddType(type.Name, 0, currentNamespace, scriptNames);
                    break;
                case FunctionDeclarationNode function:
                    AddFunction(function.Name, currentNamespace, scriptNames);
                    break;
            }
        }
    }

    private static void AddType(string name, int arity, string currentNamespace, HashSet<string> scriptNames)
    {
        var type = new TypeSymbol { QualifiedName = Combine(currentNamespace, name) };
        for (var index = 0; index < arity; index++)
            type.GenericParameters.Add(new GenericParamNode($"T{index}", string.Empty, new SourceLocation(string.Empty, 0, 0)));

        scriptNames.Add(NamingConvention.TypeName(type));
    }

    private static void AddFunction(string name, string currentNamespace, HashSet<string> scriptNames)
    {
        var organizer = new FileOrganizer(string.Empty);
        scriptNames.Add(Path.GetFileNameWithoutExtension(organizer.GetFunctionPath(name, currentNamespace)));
    }

    private static bool IsBclFile(string filePath, string bclPath)
    {
        var file = Path.GetFullPath(filePath);
        var root = Path.GetFullPath(bclPath);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
            root += Path.DirectorySeparatorChar;

        return file.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
