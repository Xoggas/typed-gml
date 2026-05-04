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
    public const string InstParam = "inst";

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
    public EmissionNarrowingState Narrowing { get; set; } = new();
    public MemberSymbol? CurrentMember { get; set; }
    public string? SelfName { get; set; }
    public bool IsObjectEventContext { get; set; }
    public bool IsInObjectMethod { get; set; }
    public bool IsInConstructor { get; set; }

    public int TempVarCounter
    {
        get => TempVariables.Counter;
        set => TempVariables.Counter = value;
    }

    public bool CanEmitTempPrelude => TempVariables.PreludeDepth > 0 && TempVariables.SuppressDepth == 0;

    internal Action<TypedGML.Compiler.Ast.IAstNode, EmitContext> Dispatch { get; } = dispatch;

    internal List<IReadOnlyDictionary<string, IAstNode>> SubstitutionFrames { get; set; } = [];

    public TypeSymbol? BaseCallLookupType { get; set; }

    public string? BaseCallReturnTarget { get; set; }

    private List<string> ExpectedTypeStack { get; set; } = [];

    internal TempVariableState TempVariables { get; private set; } = new();

    public string? CurrentExpectedType => ExpectedTypeStack.Count == 0 ? null : ExpectedTypeStack[^1];

    public string RenderWithExpected(IAstNode node, string? expectedType)
    {
        PushExpectedType(expectedType);
        try
        {
            return Emitter.Render(node, this);
        }
        finally
        {
            PopExpectedType();
        }
    }

    public void PushExpectedType(string? typeRef) =>
        ExpectedTypeStack.Add(typeRef ?? string.Empty);

    public void PopExpectedType() =>
        ExpectedTypeStack.RemoveAt(ExpectedTypeStack.Count - 1);

    public void PushSubstitution(Dictionary<string, IAstNode> substitutions) =>
        SubstitutionFrames.Add(substitutions);

    public void PopSubstitution() =>
        SubstitutionFrames.RemoveAt(SubstitutionFrames.Count - 1);

    public bool TryGetSubstitution(string name, out IAstNode substitution)
    {
        for (var i = SubstitutionFrames.Count - 1; i >= 0; i--)
            if (SubstitutionFrames[i].TryGetValue(name, out substitution!))
                return true;

        substitution = null!;
        return false;
    }

    public string RenderSubstitution(string name, IAstNode substitution)
    {
        var index = FindSubstitutionFrame(name);
        if (index < 0)
            return Emitter.Render(substitution, this);

        var frame = SubstitutionFrames[index];
        SubstitutionFrames.RemoveAt(index);
        try
        {
            return Emitter.Render(substitution, this);
        }
        finally
        {
            SubstitutionFrames.Insert(index, frame);
        }
    }

    internal EmitContext WithWriter(GmlWriter newWriter) =>
        new(Symbols, newWriter, Files, Output, Decorators, Diagnostics, Dispatch)
        {
            CurrentType = CurrentType,
            CurrentNamespacePrefix = CurrentNamespacePrefix,
            UsingPrefixes = UsingPrefixes,
            TypeDeclarations = TypeDeclarations,
            Scope = Scope,
            Narrowing = Narrowing,
            CurrentMember = CurrentMember,
            SelfName = SelfName,
            IsObjectEventContext = IsObjectEventContext,
            IsInObjectMethod = IsInObjectMethod,
            IsInConstructor = IsInConstructor,
            SubstitutionFrames = SubstitutionFrames,
            BaseCallLookupType = BaseCallLookupType,
            BaseCallReturnTarget = BaseCallReturnTarget,
            ExpectedTypeStack = ExpectedTypeStack,
            TempVariables = TempVariables,
        };

    private int FindSubstitutionFrame(string name)
    {
        for (var i = SubstitutionFrames.Count - 1; i >= 0; i--)
            if (SubstitutionFrames[i].ContainsKey(name))
                return i;

        return -1;
    }
}
