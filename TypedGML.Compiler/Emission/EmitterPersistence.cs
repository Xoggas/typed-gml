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
        var path = type is null ? null : files.GetScriptPath(type);
        if (string.IsNullOrWhiteSpace(path))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, ctx.Writer.GetOutput());
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
}
