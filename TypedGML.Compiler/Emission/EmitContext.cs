using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Ast;

namespace TypedGML.Compiler.Emission;

public sealed class EmitContext(
    SymbolTable symbols,
    GmlWriter writer,
    FileOrganizer files,
    IGmlOutputSink output,
    DecoratorAnnotations decorators,
    DiagnosticBag diagnostics,
    Action<TypedGML.Compiler.Ast.IAstNode, EmitContext> dispatch)
{
    public SymbolTable Symbols { get; } = symbols;

    public NodeEmitterFacade Emitter { get; } = new(dispatch);

    public GmlWriter Writer { get; } = writer;

    public Type Naming { get; } = typeof(NamingConvention);

    public FileOrganizer Files { get; } = files;

    public IGmlOutputSink Output { get; } = output;

    public TypeSymbol? CurrentType { get; set; }

    public string? CurrentNamespacePrefix { get; set; }

    public DecoratorAnnotations Decorators { get; set; } = decorators;

    public DiagnosticBag Diagnostics { get; } = diagnostics;

    public IReadOnlyList<string> UsingPrefixes { get; set; } = [];

    public IReadOnlyDictionary<string, IAstNode> TypeDeclarations { get; set; } = new Dictionary<string, IAstNode>(StringComparer.Ordinal);

    public ScopeStack Scope { get; set; } = new();

    public MemberSymbol? CurrentMember { get; set; }

    public string? SelfName { get; set; }

    public bool IsObjectEventContext { get; set; }

    internal Action<TypedGML.Compiler.Ast.IAstNode, EmitContext> Dispatch { get; } = dispatch;

    internal EmitContext WithWriter(GmlWriter newWriter) =>
        new(Symbols, newWriter, Files, Output, Decorators, Diagnostics, Dispatch)
        {
            CurrentType = CurrentType,
            CurrentNamespacePrefix = CurrentNamespacePrefix,
            UsingPrefixes = UsingPrefixes,
            TypeDeclarations = TypeDeclarations,
            Scope = Scope,
            CurrentMember = CurrentMember,
            SelfName = SelfName,
            IsObjectEventContext = IsObjectEventContext,
        };
}
