using System.IO;
using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

internal static class EmitterPersistence
{
    public static void Persist(IAstNode node, EmitContext ctx, FileOrganizer files, SymbolTable symbols)
    {
        var type = ResolveType(node, ctx, symbols);
        var path = node switch
        {
            FunctionDeclarationNode function => files.GetFunctionPath(function.Name, ctx.CurrentNamespacePrefix),
            _ => type is null ? null : files.GetScriptPath(type)
        };
        if (string.IsNullOrWhiteSpace(path))
            return;

        PersistToPath(path, ctx.Writer.GetOutput());
    }

    internal static void PersistToPath(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Normalize(content));
    }

    private static TypeSymbol? ResolveType(IAstNode node, EmitContext ctx, SymbolTable symbols) => node switch
    {
        ClassDeclarationNode n => Resolve(symbols, n.Name, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, ctx.CurrentNamespacePrefix),
        StructDeclarationNode n => Resolve(symbols, n.Name, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, ctx.CurrentNamespacePrefix),
        EnumDeclarationNode n => Resolve(symbols, n.Name, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, ctx.CurrentNamespacePrefix),
        _ => null
    };

    private static TypeSymbol? Resolve(SymbolTable symbols, string name, string? currentNamespace) =>
        symbols.TryResolve(name, currentNamespace, [], out var type) ? type : null;

    private static TypeSymbol Fallback(string name, string? currentNamespace) =>
        new() { QualifiedName = string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}" };

    private static string Normalize(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var output = new List<string>(lines.Length);
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("function ", StringComparison.Ordinal) &&
                output.Count > 0 &&
                !string.IsNullOrEmpty(output[^1]))
                output.Add(string.Empty);
            output.Add(line);
        }

        while (output.Count > 0 && string.IsNullOrEmpty(output[^1]))
            output.RemoveAt(output.Count - 1);

        return string.Join(Environment.NewLine, output);
    }
}
