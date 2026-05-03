using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
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

        ctx.Output.Write(path, ctx.Writer.GetOutput());
    }

    private static TypeSymbol? ResolveType(IAstNode node, EmitContext ctx, SymbolTable symbols) => node switch
    {
        ClassDeclarationNode n => Resolve(symbols, n.Name, n.GenericParams.Count, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, n.GenericParams.Count, ctx.CurrentNamespacePrefix),
        StructDeclarationNode n => Resolve(symbols, n.Name, n.GenericParams.Count, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, n.GenericParams.Count, ctx.CurrentNamespacePrefix),
        EnumDeclarationNode n => Resolve(symbols, n.Name, ctx.CurrentNamespacePrefix) ?? Fallback(n.Name, ctx.CurrentNamespacePrefix),
        _ => null
    };

    private static TypeSymbol? Resolve(SymbolTable symbols, string name, int arity, string? currentNamespace) =>
        symbols.TryResolve(name, arity, currentNamespace, [], out var type) ? type : null;

    private static TypeSymbol? Resolve(SymbolTable symbols, string name, string? currentNamespace) =>
        symbols.TryResolve(name, currentNamespace, [], out var type) ? type : null;

    private static TypeSymbol Fallback(string name, int arity, string? currentNamespace)
    {
        var symbol = Fallback(name, currentNamespace);
        for (var i = 0; i < arity; i++)
            symbol.GenericParameters.Add(new($"T{i}", string.Empty, new SourceLocation(string.Empty, 0, 0)));
        return symbol;
    }

    private static TypeSymbol Fallback(string name, string? currentNamespace) =>
        new() { QualifiedName = string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}" };
}
