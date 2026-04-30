using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

public sealed class VerificationContext(SymbolTable symbols, ScopeStack scope, DiagnosticBag diagnostics)
{
    private readonly Stack<string> _returnTypeStack = [];
    private readonly List<string> _expectedTypeStack = [];
    private readonly Dictionary<string, string> _narrowedTypes = new(StringComparer.Ordinal);

    public SymbolTable Symbols { get; } = symbols;

    public ScopeStack Scope { get; } = scope;

    public TypeSymbol? CurrentType { get; set; }

    public MemberSymbol? CurrentMember { get; set; }

    public bool IsInConstructor { get; set; }

    public bool IsInLoop { get; set; }

    public bool IsInSwitch { get; set; }

    public DiagnosticBag Diagnostics { get; } = diagnostics;

    public IReadOnlyList<string> UsingPrefixes { get; set; } = [];

    public string CurrentReturnType => _returnTypeStack.Count == 0 ? CurrentMember?.ReturnType ?? "void" : _returnTypeStack.Peek();

    public string? CurrentExpectedType => _expectedTypeStack.Count == 0 ? null : _expectedTypeStack[^1];

    public void PushReturnType(string? typeRef) =>
        _returnTypeStack.Push(string.IsNullOrWhiteSpace(typeRef) ? "void" : typeRef);

    public void PopReturnType() =>
        _returnTypeStack.Pop();

    public void PushExpectedType(string? typeRef) =>
        _expectedTypeStack.Add(typeRef ?? string.Empty);

    public void PopExpectedType() =>
        _expectedTypeStack.RemoveAt(_expectedTypeStack.Count - 1);

    public bool TryResolveNarrowedType(string name, out string typeRef) =>
        _narrowedTypes.TryGetValue(name, out typeRef!);

    public void NarrowVariable(string name, string typeRef) =>
        _narrowedTypes[name] = typeRef;

    public void ClearNarrowing(string name) =>
        _narrowedTypes.Remove(name);

    public void WithNarrowingScope(Action action)
    {
        var snapshot = new Dictionary<string, string>(_narrowedTypes, StringComparer.Ordinal);
        try
        {
            action();
        }
        finally
        {
            _narrowedTypes.Clear();
            foreach (var entry in snapshot)
                _narrowedTypes[entry.Key] = entry.Value;
        }
    }
}
