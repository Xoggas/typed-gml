using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

public sealed class VerificationContext(SymbolTable symbols, ScopeStack scope, DiagnosticBag diagnostics)
{
    public SymbolTable Symbols { get; } = symbols;

    public ScopeStack Scope { get; } = scope;

    public TypeSymbol? CurrentType { get; set; }

    public MemberSymbol? CurrentMember { get; set; }

    public bool IsInConstructor { get; set; }

    public bool IsInLoop { get; set; }

    public bool IsInSwitch { get; set; }

    public DiagnosticBag Diagnostics { get; } = diagnostics;

    public IReadOnlyList<string> UsingPrefixes { get; set; } = [];
}
