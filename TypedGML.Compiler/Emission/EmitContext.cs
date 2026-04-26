using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

public sealed class EmitContext(
    SymbolTable symbols,
    GmlWriter writer,
    FileOrganizer files,
    DecoratorAnnotations decorators,
    DiagnosticBag diagnostics,
    Action<TypedGML.Compiler.Ast.IAstNode, EmitContext> dispatch)
{
    public SymbolTable Symbols { get; } = symbols;

    public NodeEmitterFacade Emitter { get; } = new(dispatch);

    public GmlWriter Writer { get; } = writer;

    public Type Naming { get; } = typeof(NamingConvention);

    public FileOrganizer Files { get; } = files;

    public TypeSymbol? CurrentType { get; set; }

    public string? CurrentNamespacePrefix { get; set; }

    public DecoratorAnnotations Decorators { get; set; } = decorators;

    public DiagnosticBag Diagnostics { get; } = diagnostics;

    internal Action<TypedGML.Compiler.Ast.IAstNode, EmitContext> Dispatch { get; } = dispatch;
}
