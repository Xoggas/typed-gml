# TypedGML Compiler — Architecture & Semantic Checks
**Target:** .NET 10.0  
**Parser/Lexer:** ANTLR4 (`Antlr4.Runtime.Standard`)  
**Philosophy:** flat, modular, atomic. No class exceeds ~150 lines. No doc comments. SOLID throughout.

---

## Table of Contents
1. [Coding Conventions](#1-coding-conventions)
2. [Project Structure](#2-project-structure)
3. [Key Interfaces](#3-key-interfaces)
4. [Data Flow](#4-data-flow)
5. [BCL Integration](#5-bcl-integration)
6. [Phase 1 — Population](#6-phase-1--population)
7. [Phase 2 — Verification: Semantic Checks](#7-phase-2--verification-semantic-checks)
8. [Phase 3 — Emission](#8-phase-3--emission)
9. [Diagnostics](#9-diagnostics)
10. [Wiring & Composition Root](#10-wiring--composition-root)

---

## 1. Coding Conventions

- **No class longer than ~150 lines.** If a class grows past that, split by responsibility.
- **No XML doc comments.** Readability through naming, not prose.
- **No static classes** except pure utility functions with no state (`NamingConvention`, `TypeHelper`).
- **One public type per file.** File name = type name.
- **SOLID:**
  - SRP: every class has exactly one reason to change.
  - OCP: add behavior by adding new types, not modifying existing ones (especially checks and emitters).
  - LSP: subtypes are interchangeable with their abstraction.
  - ISP: interfaces are small and focused (never more than ~5 members).
  - DIP: high-level orchestrators depend on interfaces, not concrete implementations.
- **Prefer immutable data.** AST nodes are records. Symbol data is set during Population and read-only after.
- **Explicit over implicit.** No reflection-based dispatch. Registration is explicit in the composition root.
- **Errors never throw.** All diagnostic output goes through `DiagnosticBag`. Exceptions are only for genuine programmer errors (invariant violations).

---

## 2. Project Structure

```
TypedGML.Compiler/
│
├── Program.cs                            // CLI entry point, composition root
├── CompilerPipeline.cs                   // Orchestrates all three phases
├── CompilerOptions.cs                    // Input path, output path, BCL path, flags
│
├── Ast/                                  // Immutable AST node definitions (records)
│   ├── IAstNode.cs
│   ├── FileNode.cs
│   ├── Declarations/
│   │   ├── ClassDeclarationNode.cs
│   │   ├── StructDeclarationNode.cs
│   │   ├── InterfaceDeclarationNode.cs
│   │   ├── EnumDeclarationNode.cs
│   │   ├── DelegateDeclarationNode.cs
│   │   ├── NamespaceDeclarationNode.cs
│   │   └── UsingDirectiveNode.cs
│   ├── Members/
│   │   ├── FieldDeclarationNode.cs
│   │   ├── PropertyDeclarationNode.cs
│   │   ├── MethodDeclarationNode.cs
│   │   ├── ConstructorDeclarationNode.cs
│   │   ├── IndexerDeclarationNode.cs
│   │   ├── OperatorDeclarationNode.cs
│   │   └── EventDeclarationNode.cs
│   ├── Statements/
│   │   ├── BlockStatementNode.cs
│   │   ├── VarDeclarationStatementNode.cs
│   │   ├── IfStatementNode.cs
│   │   ├── WhileStatementNode.cs
│   │   ├── ForStatementNode.cs
│   │   ├── RepeatStatementNode.cs
│   │   ├── SwitchStatementNode.cs
│   │   ├── WithStatementNode.cs
│   │   ├── ReturnStatementNode.cs
│   │   ├── BreakStatementNode.cs
│   │   ├── ContinueStatementNode.cs
│   │   ├── TryStatementNode.cs
│   │   ├── ThrowStatementNode.cs
│   │   └── ExpressionStatementNode.cs
│   └── Expressions/
│       ├── BinaryExpressionNode.cs
│       ├── UnaryExpressionNode.cs
│       ├── TernaryExpressionNode.cs
│       ├── AssignmentExpressionNode.cs
│       ├── MemberAccessExpressionNode.cs
│       ├── IndexerAccessExpressionNode.cs
│       ├── InvocationExpressionNode.cs
│       ├── ObjectCreationExpressionNode.cs
│       ├── LambdaExpressionNode.cs
│       ├── CastExpressionNode.cs           // as / is
│       ├── TypeofExpressionNode.cs
│       ├── NameofExpressionNode.cs
│       ├── DefaultExpressionNode.cs
│       ├── NullCoalescingExpressionNode.cs
│       ├── NullConditionalExpressionNode.cs
│       ├── ArrayLiteralExpressionNode.cs
│       ├── IdentifierExpressionNode.cs
│       └── LiteralExpressionNode.cs
│
├── Parsing/
│   ├── AstBuilder.cs                     // ANTLR visitor → AST nodes
│   └── ParseErrorListener.cs             // Converts ANTLR errors → DiagnosticBag
│
├── Symbols/
│   ├── SymbolTable.cs                    // Global registry: qualified name → TypeSymbol
│   ├── TypeSymbol.cs                     // Type descriptor: kind, members, base, interfaces
│   ├── MemberSymbol.cs                   // Field/method/property/event descriptor
│   ├── ParameterSymbol.cs
│   ├── ScopeStack.cs                     // Lexical scope chain for local variables
│   ├── TypeKind.cs                       // Enum: Class | Struct | Interface | Enum | Delegate | Primitive
│   └── BuiltinTypeRegistry.cs            // Primitive type symbols: number, string, bool, void, null, object
│
├── Population/
│   ├── Populator.cs                      // Orchestrates all populators in order
│   ├── NamespacePopulator.cs             // Registers namespaces, resolves using directives
│   ├── TypePopulator.cs                  // Registers class/struct/interface/enum/delegate symbols
│   ├── MemberPopulator.cs                // Registers fields, methods, properties per type
│   ├── InheritanceResolver.cs            // Builds base-type and interface chains, detects cycles
│   └── GenericParameterBinder.cs         // Resolves generic param names in member signatures
│
├── Verification/
│   ├── Verifier.cs                       // Walks all nodes; dispatches to registered ISemanticCheck list
│   ├── VerificationContext.cs            // Shared state: SymbolTable, ScopeStack, current type, DiagnosticBag
│   ├── ISemanticCheck.cs                 // Interface: bool Matches(IAstNode); void Check(IAstNode, VerificationContext)
│   │
│   └── Checks/
│       ├── TypeAssignabilityCheck.cs     // Assignment, return, argument type compatibility
│       ├── NullabilityCheck.cs           // null → non-nullable, T? used as T without unwrap
│       ├── MemberAccessCheck.cs          // Undeclared member, private/protected violations
│       ├── MethodCallCheck.cs            // Arg count, named args order/duplicate, type match
│       ├── ConstructorCallCheck.cs       // base()/this() chain validity, arg types
│       ├── OperatorCheck.cs              // Arithmetic/logical/comparison operand types, overload resolution
│       ├── ControlFlowCheck.cs           // break/continue scope, return in all paths, unreachable code
│       ├── AbstractCompletenessCheck.cs  // All abstract members overridden, no abstract instantiation
│       ├── InterfaceImplementationCheck.cs // All interface members present with correct signatures
│       ├── OverrideSignatureCheck.cs     // override matches virtual/abstract, sealed not overridden
│       ├── SealedInheritanceCheck.cs     // Sealed class not subclassed
│       ├── StructInheritanceCheck.cs     // Struct cannot inherit from non-object
│       ├── GenericConstraintCheck.cs     // Type args satisfy constraints at instantiation sites
│       ├── DelegateSignatureCheck.cs     // += / -= signature match, invocation arg types
│       ├── EventAccessCheck.cs           // Event assigned directly from outside declaring type
│       ├── LambdaCheck.cs                // No closure capture, type must be inferrable from context
│       ├── DecoratorPlacementCheck.cs    // @Object on class only, @NativeEvent on method only, etc.
│       ├── ObjectDecoratorCheck.cs       // Duplicate @Object, new @Object class arg count
│       ├── ConstExpressionCheck.cs       // const RHS must be compile-time constant
│       ├── ReadonlyAssignmentCheck.cs    // readonly field only assigned in constructor
│       ├── StaticConstructorCheck.cs     // static ctor rules: no params, no access mod, no cross-class deps
│       ├── StaticModifierCheck.cs        // static anywhere → error (user code)
│       ├── WithTargetCheck.cs            // with only on @Object instances
│       ├── SwitchCaseConstantCheck.cs    // case labels must be compile-time constants
│       ├── DuplicateCaseCheck.cs         // duplicate switch case labels
│       ├── VarInferenceCheck.cs          // var RHS not null, type must be unambiguous
│       ├── ArrayLiteralTypeCheck.cs      // all elements same type, infer element type
│       ├── ThrowTypeCheck.cs             // throw must be Exception
│       ├── TypeofNameCheck.cs            // typeof/nameof resolve to known types/members
│       ├── IsAsCompatibilityCheck.cs     // warn if is/as on unrelated types (always true/false)
│       ├── CyclicConstCheck.cs           // const fields referencing each other cyclically
│       ├── DuplicateMemberCheck.cs       // duplicate field/method/property names in same type
│       ├── DuplicateParameterCheck.cs    // duplicate parameter names in same signature
│       ├── DefaultParameterConstCheck.cs // default param values must be constants/literals
│       └── ObjectCreationCheck.cs        // new on interface/enum → error; abstract class → error
│
├── Emission/
│   ├── Emitter.cs                        // Orchestrates emission; calls DecoratorProcessor, then emitters
│   ├── EmitContext.cs                    // Current type, namespace, GmlWriter, SymbolTable, options
│   ├── INodeEmitter.cs                   // Interface: bool Matches(IAstNode); void Emit(IAstNode, EmitContext)
│   ├── GmlWriter.cs                      // Indented string builder for GML output
│   ├── NamingConvention.cs               // Static: all GML name derivation logic (one place, no exceptions)
│   ├── FileOrganizer.cs                  // Decides output path per node (@Object events vs scripts)
│   ├── DecoratorProcessor.cs             // Pre-emission: reads decorators, annotates EmitContext
│   │
│   └── Emitters/
│       ├── ClassEmitter.cs
│       ├── StructEmitter.cs
│       ├── EnumEmitter.cs
│       ├── DelegateEmitter.cs
│       ├── StaticCtorEmitter.cs
│       ├── ConstructorEmitter.cs
│       ├── MethodEmitter.cs
│       ├── PropertyEmitter.cs
│       ├── IndexerEmitter.cs
│       ├── OperatorEmitter.cs
│       ├── FieldEmitter.cs
│       ├── EventEmitter.cs
│       ├── Statements/
│       │   ├── BlockStatementEmitter.cs
│       │   ├── IfStatementEmitter.cs
│       │   ├── WhileStatementEmitter.cs
│       │   ├── ForStatementEmitter.cs
│       │   ├── RepeatStatementEmitter.cs
│       │   ├── SwitchStatementEmitter.cs
│       │   ├── WithStatementEmitter.cs
│       │   ├── TryStatementEmitter.cs
│       │   ├── ReturnStatementEmitter.cs
│       │   ├── ThrowStatementEmitter.cs
│       │   ├── VarDeclarationStatementEmitter.cs
│       │   └── ExpressionStatementEmitter.cs
│       └── Expressions/
│           ├── BinaryExpressionEmitter.cs
│           ├── UnaryExpressionEmitter.cs
│           ├── TernaryExpressionEmitter.cs
│           ├── AssignmentExpressionEmitter.cs
│           ├── MemberAccessExpressionEmitter.cs
│           ├── InvocationExpressionEmitter.cs
│           ├── ObjectCreationExpressionEmitter.cs
│           ├── LambdaExpressionEmitter.cs
│           ├── NullCoalescingExpressionEmitter.cs
│           ├── NullConditionalExpressionEmitter.cs
│           ├── ArrayLiteralExpressionEmitter.cs
│           ├── TypeofExpressionEmitter.cs
│           ├── NameofExpressionEmitter.cs
│           └── DefaultExpressionEmitter.cs
│
├── Bcl/
│   └── BclLoader.cs                      // Locates bundled BCL folder, returns file paths
│
├── Diagnostics/
│   ├── Diagnostic.cs                     // record: Code, Severity, Message, Location
│   ├── DiagnosticSeverity.cs             // Error | Warning
│   ├── DiagnosticCode.cs                 // Enum of all TGML00xx codes
│   ├── DiagnosticBag.cs                  // Collects diagnostics; HasErrors, All, Errors, Warnings
│   └── SourceLocation.cs                 // record: FilePath, Line, Column
│
└── Utils/
    ├── FileScanner.cs                    // Recursive .tgml discovery from root path
    └── TypeHelper.cs                     // IsAssignableTo, IsNullable, UnwrapNullable, etc.
```

---

## 3. Key Interfaces

### 3.1 ISemanticCheck

```csharp
interface ISemanticCheck
{
    bool Matches(IAstNode node);
    void Check(IAstNode node, VerificationContext ctx);
}
```

`Matches` lets the `Verifier` avoid unnecessary dispatch. Each check declares exactly which node type(s) it handles. `Verifier` iterates every node in the AST and calls every check whose `Matches` returns true.

**Pattern for concrete checks:**
```csharp
sealed class ReadonlyAssignmentCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var assignment = (AssignmentExpressionNode)node;
        // ... resolve target member, check readonly, report TGML0009
    }
}
```

### 3.2 INodeEmitter

```csharp
interface INodeEmitter
{
    bool Matches(IAstNode node);
    void Emit(IAstNode node, EmitContext ctx);
}
```

Same pattern as checks. The `Emitter` orchestrator dispatches to the first matching `INodeEmitter`.

### 3.3 VerificationContext

```csharp
sealed class VerificationContext
{
    public SymbolTable Symbols { get; }
    public ScopeStack Scope { get; }
    public TypeSymbol? CurrentType { get; set; }
    public MemberSymbol? CurrentMember { get; set; }
    public bool IsInConstructor { get; set; }
    public bool IsInLoop { get; set; }
    public bool IsInSwitch { get; set; }
    public DiagnosticBag Diagnostics { get; }
}
```

Context is mutable and passed by reference through all checks. Checks update `CurrentType`, `IsInLoop`, etc. as they descend into nested scopes.

### 3.4 EmitContext

```csharp
sealed class EmitContext
{
    public SymbolTable Symbols { get; }
    public GmlWriter Writer { get; }
    public NamingConvention Naming { get; }
    public FileOrganizer Files { get; }
    public TypeSymbol? CurrentType { get; set; }
    public string? CurrentNamespacePrefix { get; set; }
    public DecoratorAnnotations Decorators { get; }  // set by DecoratorProcessor
}
```

### 3.5 IAstNode

```csharp
interface IAstNode
{
    SourceLocation Location { get; }
}
```

All AST nodes are `record` types implementing this interface.

---

## 4. Data Flow

```
CLI args
  └─► CompilerOptions

FileScanner
  └─► string[] allFilePaths         (BCL files first, then user files)

BclLoader
  └─► string[] bclFilePaths         (bundled alongside executable)

ANTLR4 Lexer+Parser (per file)
  └─► AstBuilder.Visit(parseTree)
        └─► FileNode[]              (one per file)

Populator
  ├─► NamespacePopulator            → SymbolTable (namespaces, using maps)
  ├─► TypePopulator                 → SymbolTable (TypeSymbol per type)
  ├─► MemberPopulator               → TypeSymbol.Members
  ├─► InheritanceResolver           → TypeSymbol.Base, TypeSymbol.Interfaces
  └─► GenericParameterBinder        → MemberSymbol.Parameters (types resolved)

  If DiagnosticBag.HasErrors → print & exit

Verifier
  ├─► builds VerificationContext
  ├─► walks every IAstNode in all FileNodes (depth-first)
  └─► foreach check in _checks: if check.Matches(node) → check.Check(node, ctx)

  If DiagnosticBag.HasErrors → print & exit

DecoratorProcessor
  └─► annotates EmitContext.Decorators from all decorator nodes

Emitter
  ├─► builds EmitContext
  ├─► foreach top-level node → dispatch to matching INodeEmitter
  │     each emitter recursively emits its children via ctx.Writer
  └─► FileOrganizer writes GmlWriter output to correct .gml paths

Output .gml files written to disk
```

---

## 5. BCL Integration

### 5.1 Loading Order

`FileScanner` is called twice in `CompilerPipeline`:
1. `BclLoader.GetFiles()` → BCL `.tgml` files from the bundled `bcl/` folder adjacent to the executable.
2. `FileScanner.Scan(options.InputPath)` → user project files.

BCL files are prepended to the file list. Population processes all of them together, so BCL types are available when resolving user types.

### 5.2 Hardcoded Primitive Knowledge

`BuiltinTypeRegistry` is populated in code, not from BCL files. It covers only:

- Primitive type symbols (`number`, `string`, `bool`, `void`, `null`, `object`) registered with `TypeKind.Primitive`.
- `object.ToString()` and `GetType()` method signatures.
- The three `BclTypeName` aliases: `number` → `"Number"`, `string` → `"String"`, `bool` → `"Bool"`.

**Operator validity for primitive types is NOT hardcoded.** It is defined entirely in BCL files (`Number.tgml`, `String.tgml`, `Bool.tgml`) using the same `static operator` syntax available to user code. `OperatorCheck` resolves operators for primitive types exactly the same way it resolves user-defined operator overloads — by looking up `operator +` (etc.) in `SymbolTable` on the relevant BCL type.

`PrimitiveOperationRegistry` does not exist. It must not be created.

BCL types (`List<T>`, `Dictionary<K,V>`, `Exception`, `Math`, `Number`, `String`, `Bool`, etc.) are all defined in `.tgml` BCL files and go through the normal Population phase. The compiler treats them as ordinary types, with two special exceptions hardcoded by name:
- `Exception` — recognized for `throw`/`catch` checks.
- `Number`, `String`, `Bool` — recognized as aliases for the `number`, `string`, `bool` keywords.

### 5.3 BclLoader

```csharp
sealed class BclLoader
{
    private readonly string _bclDirectory;  // passed from CompilerOptions or auto-detected from Assembly.Location

    public IReadOnlyList<string> GetFiles() =>
        Directory.GetFiles(_bclDirectory, "*.tgml", SearchOption.AllDirectories);
}
```

---

## 6. Phase 1 — Population

### Populator Ordering

Population runs in strict order:

1. **NamespacePopulator** — registers all `namespace` declarations. Resolves `using` directives. Builds a per-file `UsingMap: shortName → qualifiedName`.
2. **TypePopulator** — for each type declaration, creates a `TypeSymbol` and registers it in `SymbolTable` under its qualified name. Detects duplicate names (TGML0001).
3. **MemberPopulator** — for each member in each type, creates a `MemberSymbol` and attaches it to its `TypeSymbol`. At this point, parameter types are strings (unresolved).
4. **InheritanceResolver** — resolves base class and interface references by qualified name. Detects cycles (TGML0002).
5. **GenericParameterBinder** — replaces unresolved type-string references in member signatures with resolved `TypeSymbol` references, substituting generic parameters where applicable.

Each populator is injected with `SymbolTable` and `DiagnosticBag` only. They do not depend on each other directly.

### SymbolTable

```csharp
sealed class SymbolTable
{
    public void Register(string qualifiedName, TypeSymbol symbol);
    public bool TryResolve(string name, string? currentNamespace, IReadOnlyList<string> usingPrefixes, out TypeSymbol symbol);
    public IReadOnlyList<TypeSymbol> AllTypes { get; }
}
```

`TryResolve` applies `using` prefix expansion: tries `name` as-is first, then `usingPrefix + "." + name` for each active using.

---

## 7. Phase 2 — Verification: Semantic Checks

The `Verifier` performs a **single depth-first walk** of all AST nodes. For each node, it runs every `ISemanticCheck` whose `Matches` returns true. Checks update `VerificationContext` state as they descend (e.g. setting `IsInLoop = true` when entering a `WhileStatementNode`).

All errors are collected into `DiagnosticBag` before halting. No check throws exceptions.

Below is the complete catalogue of semantic checks, grouped by concern.

---

### 7.1 Type Assignability

**File:** `TypeAssignabilityCheck.cs`  
**Matches:** `AssignmentExpressionNode`, `VarDeclarationStatementNode`, `ReturnStatementNode`

| # | Rule |
|---|---|
| A01 | RHS type must be assignable to LHS declared type. |
| A02 | `return` value type must match the enclosing method's return type. |
| A03 | `return` with value in a `void` method is an error. |
| A04 | `return` without value in a non-`void` method is an error (unless covered by A05). |
| A05 | All code paths in a non-`void` method must return a value (control flow analysis). |
| A06 | `var` RHS may not be `null` literal (ambiguous type). |
| A07 | `var` RHS type must be uniquely determinable. |

---

### 7.2 Nullability

**File:** `NullabilityCheck.cs`  
**Matches:** `AssignmentExpressionNode`, `VarDeclarationStatementNode`, `InvocationExpressionNode`, `MemberAccessExpressionNode`

| # | Rule |
|---|---|
| N01 | `null` may not be assigned to a non-nullable type. |
| N02 | A `T?` value used directly as `T` (without `??` or `?.`) is an error. |
| N03 | Calling a method directly on an expression of nullable type (without `?.`) is an error. |
| N04 | Accessing a member directly on a nullable-typed expression (without `?.`) is an error. |

---

### 7.3 Member Access

**File:** `MemberAccessCheck.cs`  
**Matches:** `MemberAccessExpressionNode`, `IdentifierExpressionNode`

| # | Rule |
|---|---|
| M01 | Member must exist on the resolved type. |
| M02 | `private` member accessed from outside its declaring type → error. |
| M03 | `protected` member accessed from outside the inheritance chain → error. |
| M04 | `this` used outside an instance context (top-level function) → error. |
| M05 | Local variable used before declaration in its scope → error. |
| M06 | Undeclared identifier in expression → error. |

---

### 7.4 Method and Function Calls

**File:** `MethodCallCheck.cs`  
**Matches:** `InvocationExpressionNode`

| # | Rule |
|---|---|
| C01 | Number of supplied positional arguments must not exceed the number of parameters (accounting for defaults). |
| C02 | All required parameters (no default) must be supplied. |
| C03 | Named argument name must match an existing parameter name. |
| C04 | Named arguments must not duplicate each other or a positional argument already covering that parameter. |
| C05 | Positional arguments must not appear after named arguments. |
| C06 | Each argument's type must be assignable to the corresponding parameter's type. |
| C07 | Calling a non-callable expression (not a method or delegate) → error. |

---

### 7.5 Constructor Calls

**File:** `ConstructorCallCheck.cs`  
**Matches:** `ConstructorDeclarationNode`, `ObjectCreationExpressionNode`

| # | Rule |
|---|---|
| K01 | `base(...)` argument count and types must match the base class constructor. |
| K02 | `this(...)` argument count and types must match another constructor in the same class. |
| K03 | `this(...)` chaining must not be circular. |
| K04 | `new Interface(...)` → error (interfaces are not instantiable). |
| K05 | `new AbstractClass(...)` → error. |
| K06 | `new EnumType(...)` → error. |
| K07 | `new @ObjectClass(x, y, layer, ...)` — the first three args must be present; additional args mapped to constructor params. |

---

### 7.6 Operator Types

**File:** `OperatorCheck.cs`  
**Matches:** `BinaryExpressionNode`, `UnaryExpressionNode`

All operator resolution goes through `SymbolTable`. Primitive types (`number`, `string`, `bool`) define their operators in BCL files (`Number.tgml`, `String.tgml`, `Bool.tgml`) using `static operator` declarations. `OperatorCheck` treats primitive types identically to user-defined types — no special cases.

| # | Rule |
|---|---|
| O01 | For a binary expression `left op right`: resolve the left operand's type. Look up `operator op` with matching parameter types in `SymbolTable`. If found, the result type is the operator's return type. If not found, try the right operand's type. If neither has a matching operator → error. |
| O02 | For a unary expression `op operand`: look up `operator op` on the operand's type in `SymbolTable`. If not found → error. |
| O03 | `and`, `or`, `not` are logical operators — they are NOT overloadable. They always require `bool` operands (verified by direct type check, not operator lookup). Result is always `bool`. |
| O04 | Implicit `number` → `bool` is forbidden. A `bool` context (if/while/for condition, `and`/`or`/`not` operand) that receives a `number` → error, even if `number` has a conversion operator to `bool`. |
| O05 | `implicit` conversion: when a type mismatch occurs at an assignment or argument site, check for `static implicit operator TargetType(SourceType)` on either type. If found, allow and emit the conversion call. |
| O06 | `explicit` conversion (`expr as TargetType` cast form): compile-time only, no runtime check emitted. |
| O07 | `==` and `!=` on types with no `operator ==` defined: fall back to reference equality (valid for any type). |

---

### 7.7 Control Flow

**File:** `ControlFlowCheck.cs`  
**Matches:** `BreakStatementNode`, `ContinueStatementNode`, `ReturnStatementNode`, `WhileStatementNode`, `ForStatementNode`, `RepeatStatementNode`, `SwitchStatementNode`

| # | Rule |
|---|---|
| F01 | `break` outside a loop or `switch` → error. |
| F02 | `continue` outside a loop → error. |
| F03 | Statements after `return`/`break`/`continue` in the same block are unreachable → warning. |
| F04 | A `switch` without `default` and without exhaustive cases in a non-void context → warning. |
| F05 | Fall-through between switch cases (no `break` at end of non-empty case) → error. |
| F06 | `for` condition (if present) must be `bool`. |
| F07 | `while` condition must be `bool`. |
| F08 | `if` condition must be `bool`. |
| F09 | `repeat` count must be `number`. |
| F10 | `ternary` condition must be `bool`. |

---

### 7.8 Abstract Completeness

**File:** `AbstractCompletenessCheck.cs`  
**Matches:** `ClassDeclarationNode`

| # | Rule |
|---|---|
| AB01 | A concrete (non-abstract) class that inherits an abstract class must implement every `abstract` member. |
| AB02 | An `abstract` method/property must have no body. |
| AB03 | A non-abstract method must have a body. |

---

### 7.9 Interface Implementation

**File:** `InterfaceImplementationCheck.cs`  
**Matches:** `ClassDeclarationNode`, `StructDeclarationNode`

| # | Rule |
|---|---|
| I01 | Every method, property, indexer, and event declared in an implemented interface must appear in the implementing type with a matching signature. |
| I02 | A class implementing an interface with an `event` must declare that event. |
| I03 | Interface member signature (parameter types, return type, nullability) must match exactly. |

---

### 7.10 Override and Sealed

**File:** `OverrideSignatureCheck.cs`  
**Matches:** `MethodDeclarationNode`, `PropertyDeclarationNode`

| # | Rule |
|---|---|
| V01 | `override` must have a matching `virtual` or `abstract` ancestor (same name + compatible signature). |
| V02 | Signature of `override` (parameter types, return type) must match the overridden member exactly. |
| V03 | `virtual` or `override` on a struct member → error (structs cannot have virtual dispatch). |
| V04 | Overriding a `sealed` member in a base class → error. |

**File:** `SealedInheritanceCheck.cs`  
**Matches:** `ClassDeclarationNode`

| # | Rule |
|---|---|
| V05 | Inheriting from a `sealed` class → error. |

**File:** `StructInheritanceCheck.cs`  
**Matches:** `StructDeclarationNode`

| # | Rule |
|---|---|
| V06 | Struct declaring a non-interface base type → error. |

---

### 7.11 Generic Constraints

**File:** `GenericConstraintCheck.cs`  
**Matches:** `ObjectCreationExpressionNode`, `InvocationExpressionNode`, `MemberAccessExpressionNode` (where generics are instantiated)

| # | Rule |
|---|---|
| G01 | Each supplied type argument must satisfy the constraint declared on the corresponding generic parameter. |
| G02 | Wrong number of type arguments supplied → error. |
| G03 | Type argument constraint is an interface → supplied type must implement that interface. |
| G04 | Type argument constraint is a class → supplied type must be that class or a subclass. |
| G05 | Type argument constraint is a struct → supplied type must be that struct. |

---

### 7.12 Delegates and Events

**File:** `DelegateSignatureCheck.cs`  
**Matches:** `AssignmentExpressionNode` (where operator is `+=`/`-=`), `InvocationExpressionNode` (delegate calls)

| # | Rule |
|---|---|
| D01 | The method or lambda on the RHS of `+=`/`-=` must match the delegate's parameter types and return type. |
| D02 | Invoking a delegate with wrong argument count or types → error. |
| D03 | `+=`/`-=` on a non-delegate/non-event type → error. |

**File:** `EventAccessCheck.cs`  
**Matches:** `AssignmentExpressionNode`

| # | Rule |
|---|---|
| D04 | Direct assignment (`=`) to an `event` from outside the declaring type → error (only `+=`/`-=` allowed). |

---

### 7.13 Lambdas

**File:** `LambdaCheck.cs`  
**Matches:** `LambdaExpressionNode`

| # | Rule |
|---|---|
| L01 | Lambda may not reference any variable from the enclosing scope (no closures). |
| L02 | Lambda must be assignable to an inferred or declared delegate type (Action/Func/custom). |
| L03 | Lambda parameter count must match the target delegate's parameter count. |
| L04 | Lambda parameter types (if annotated) must match the target delegate's parameter types. |

---

### 7.14 Decorator Placement

**File:** `DecoratorPlacementCheck.cs`  
**Matches:** all declaration nodes with decorator lists

| # | Rule |
|---|---|
| DP01 | `@Object` only on `ClassDeclarationNode`. |
| DP02 | `@NativeEvent` only on `MethodDeclarationNode`. |
| DP03 | `@NativeProperty` only on `PropertyDeclarationNode`. |
| DP04 | `@NativeCall` only on `MethodDeclarationNode`. |
| DP05 | `@Asset` only on `PropertyDeclarationNode`. |

**File:** `ObjectDecoratorCheck.cs`  
**Matches:** `ClassDeclarationNode`

| # | Rule |
|---|---|
| DP06 | Multiple `@Object` decorators on one class → error. |
| DP07 | `@Object` string argument must be a non-empty string literal. |

**File:** `GameObjectDecoratorCheck.cs`
**Matches:** `ClassDeclarationNode`

| # | Rule |
|---|---|
| DP08 | Concrete classes derived from `TypedGML.GameObjects.GameObject` must have `@Object` → error `TGML0045`. |
| DP09 | Classes with `@Object` must explicitly derive from `TypedGML.GameObjects.GameObject` → error `TGML0046`. |
| DP10 | Abstract classes derived from `TypedGML.GameObjects.GameObject` must not have `@Object` → error `TGML0050`. |

---

### 7.15 Const and Readonly

**File:** `ConstExpressionCheck.cs`  
**Matches:** `FieldDeclarationNode` where `const` modifier present

| # | Rule |
|---|---|
| CR01 | `const` initializer must be a compile-time constant: number literal, string literal, bool literal, or reference to another `const`. |
| CR02 | `const` field must have an initializer. |
| CR03 | Circular `const` reference chain → error. |

**File:** `ReadonlyAssignmentCheck.cs`  
**Matches:** `AssignmentExpressionNode`

| # | Rule |
|---|---|
| CR04 | `readonly` field may only be assigned in the constructor of its declaring type. Any other assignment → error. |

---

### 7.16 Modifier Validation

**File:** `StaticModifierCheck.cs`  
**Matches:** declaration nodes with `static` modifier

| # | Rule |
|---|---|
| MD01 | `static` on a constructor (non-static) → error TGML0036. |
| MD02 | `static` on an indexer → error TGML0036. |
| MD03 | `static` member inside an interface → error TGML0037. |

**File:** `StaticConstructorCheck.cs`  
**Matches:** `ClassDeclarationNode`, `StructDeclarationNode`

| # | Rule |
|---|---|
| SC01 | More than one static constructor in the same type → error TGML0043. |
| SC02 | Static constructor has parameters → error. |
| SC03 | Static constructor has an access modifier → error. |
| SC04 | Static constructor body references `this` or any instance member → error. |
| SC05 | Static constructor body references a static member of a different class → error TGML0044. |
| SC06 | Static field initializer that is not a compile-time constant and references another class's static member → error TGML0044. |

---

### 7.17 With Statement

**File:** `WithTargetCheck.cs`  
**Matches:** `WithStatementNode`

| # | Rule |
|---|---|
| W01 | The target expression of `with` must resolve to a type decorated with `@Object`. Anything else → error. |

---

### 7.18 Switch

**File:** `SwitchCaseConstantCheck.cs`  
**Matches:** `SwitchStatementNode`

| # | Rule |
|---|---|
| SW01 | All `case` labels must be compile-time constants (literals, `const` field references, enum members). |

**File:** `DuplicateCaseCheck.cs`  
**Matches:** `SwitchStatementNode`

| # | Rule |
|---|---|
| SW02 | No two `case` labels in the same `switch` may have the same value. |

---

### 7.19 Var Inference

**File:** `VarInferenceCheck.cs`  
**Matches:** `VarDeclarationStatementNode`

| # | Rule |
|---|---|
| VI01 | `var` with RHS of `null` literal → error (ambiguous type). |
| VI02 | `var` where RHS type cannot be resolved → error. |

---

### 7.20 Dictionary Literals

**File:** `DictionaryLiteralCheck.cs`  
**Matches:** `DictionaryLiteralExpressionNode`

| # | Rule |
|---|---|
| DL01 | A brace literal `{k: v, ...}` may only be assigned to a declared `Dictionary<TKey, TValue>`. Assigning to any other type → error. |
| DL02 | All keys must be of type `TKey` (from the target Dictionary's generic args). |
| DL03 | All values must be of type `TValue`. |
| DL04 | Duplicate key expressions in the same literal (same compile-time constant value) → error `TGML0042`. |
| DL05 | An empty literal `{}` is valid; it emits `ds_map_create()` with no `ds_map_add` calls. |

> `DictionaryLiteralExpressionNode` is a new AST node: list of `(IAstNode Key, IAstNode Value)` pairs. Add it to `Ast/Expressions/`.

**GML emission** is handled by a new `DictionaryLiteralExpressionEmitter.cs`:
```gml
// {"Alice": 10, "Bob": 20}
(function() {
    var __dict = ds_map_create();
    ds_map_add(__dict, "Alice", 10);
    ds_map_add(__dict, "Bob", 20);
    return __dict;
})()
```
The IIFE wrapper allows the literal to be used inline in any expression position.

### 7.22 Array Literals

**File:** `ArrayLiteralTypeCheck.cs`  
**Matches:** `ArrayLiteralExpressionNode`

| # | Rule |
|---|---|
| AR01 | All elements in an array literal must be of the same type (or mutually assignable to the inferred element type). |
| AR02 | An empty array literal `[]` must have an explicit type context (variable type, parameter type, or cast). |

---

### 7.23 Throw

**File:** `ThrowTypeCheck.cs`  
**Matches:** `ThrowStatementNode`

| # | Rule |
|---|---|
| TH01 | The expression in `throw` must be of type `Exception` (exactly — no subclassing supported). |
| TH02 | `throw` is a statement only; using it as an expression → error. |

---

### 7.24 typeof / nameof

**File:** `TypeofNameCheck.cs`  
**Matches:** `TypeofExpressionNode`, `NameofExpressionNode`

| # | Rule |
|---|---|
| TN01 | `typeof(T)` — `T` must resolve to a known type in the symbol table. |
| TN02 | `nameof(x.y.z)` — each segment of the chain must resolve: `x` is a type or variable, `y` is a member of `x`'s type, etc. |

---

### 7.25 is / as Compatibility

**File:** `IsAsCompatibilityCheck.cs`  
**Matches:** `CastExpressionNode`

| # | Rule |
|---|---|
| IA01 | `expr is TypeName` where the compile-time type of `expr` is unrelated to `TypeName` (no shared ancestor/interface) → warning: always false. |
| IA02 | `expr as TypeName` where the compile-time type of `expr` is unrelated → warning: cast is always invalid. |

---

### 7.26 Duplicate Declarations

**File:** `DuplicateMemberCheck.cs`  
**Matches:** `ClassDeclarationNode`, `StructDeclarationNode`, `InterfaceDeclarationNode`

| # | Rule |
|---|---|
| DM01 | Two fields with the same name in the same type → error. |
| DM02 | Two methods with the same name and identical parameter type lists → error (no overloading by return type). |
| DM03 | Two properties with the same name → error. |
| DM04 | Two events with the same name → error. |

**File:** `DuplicateParameterCheck.cs`  
**Matches:** `MethodDeclarationNode`, `ConstructorDeclarationNode`, `DelegateDeclarationNode`, `LambdaExpressionNode`

| # | Rule |
|---|---|
| DM05 | Two parameters with the same name in the same signature → error. |

---

### 7.27 Default Parameters

**File:** `DefaultParameterConstCheck.cs`  
**Matches:** `MethodDeclarationNode`, `ConstructorDeclarationNode`

| # | Rule |
|---|---|
| DF01 | Default parameter values must be literals or `const` field references. |
| DF02 | A parameter with a default value must not be followed by a parameter without one. |

---

### 7.28 Object Creation

**File:** `ObjectCreationCheck.cs`  
**Matches:** `ObjectCreationExpressionNode`

| # | Rule |
|---|---|
| OC01 | `new InterfaceType(...)` → error. |
| OC02 | `new AbstractClass(...)` → error. |
| OC03 | `new EnumType(...)` → error. |
| OC04 | Type must have a constructor matching the supplied argument count and types. |

---

## 8. Phase 3 — Emission

### 8.1 Emitter Orchestration

`Emitter` holds a list of `INodeEmitter` registrations. For each AST node, it calls the first matching emitter. Emitters for containers (class, method) recursively call `Emitter` for their children.

Emitters must not perform any type resolution — all that is done in Verification. Emitters only read `TypeSymbol` and `MemberSymbol` metadata from the already-populated `SymbolTable`.

### 8.2 NamingConvention

All GML name derivation lives in one `static class NamingConvention`. No emitter invents names independently.

```csharp
static class NamingConvention
{
    public static string TypeName(TypeSymbol type);                    // MyGame_Entities_Player
    public static string MethodName(TypeSymbol type, MemberSymbol method);  // MyGame_Entities_Player_Attack
    public static string ConstructorName(TypeSymbol type);             // MyGame_Entities_Player_create
    public static string PropertyGetter(TypeSymbol type, MemberSymbol prop); // MyGame_Entities_Player_get_Health
    public static string PropertySetter(TypeSymbol type, MemberSymbol prop); // MyGame_Entities_Player_set_Health
    public static string OperatorName(TypeSymbol type, string op);     // MyGame_Entities_Player_op_add
    public static string ConstMacro(TypeSymbol type, MemberSymbol field);    // MyGame_Entities_Player_MaxSpeed
    public static string EnumMember(TypeSymbol enumType, string member);     // Direction_North
    public static string GlobalProperty(MemberSymbol prop);            // global.Score
}
```

### 8.3 FileOrganizer

Decides the output `.gml` file path for each emitted unit:

```csharp
sealed class FileOrganizer
{
    public string GetScriptPath(TypeSymbol type);                  // scripts/MyGame_Entities_Player.gml
    public string GetEventPath(TypeSymbol type, string eventName); // objects/OBJ_Player/Create_0.gml
    public string GetMacroPath(string namespaceName);              // macros/MyGame_Entities_macros.gml
}
```

### 8.4 GmlWriter

Thin wrapper over `StringBuilder` with indentation tracking:

```csharp
sealed class GmlWriter
{
    public void WriteLine(string line);
    public void WriteBlock(string header, Action body);  // writes header { \n body \n }
    public void Indent();
    public void Dedent();
    public string GetOutput();
}
```

### 8.4 Static Member Emission

All `static` members emit as `global.*` entries inside a generated `ClassName_static_ctor` function. The function is registered via `gml_pragma("global", "ClassName_static_ctor()")`.

`NamingConvention` gains these methods:

```csharp
public static string StaticMemberName(TypeSymbol type, MemberSymbol member);
// global.ClassName_MemberName  (field/method variable)

public static string StaticGetterName(TypeSymbol type, MemberSymbol prop);
// global.ClassName_get_PropName

public static string StaticSetterName(TypeSymbol type, MemberSymbol prop);
// global.ClassName_set_PropName

public static string StaticCtorFunctionName(TypeSymbol type);
// ClassName_static_ctor
```

`BuiltinTypeRegistry` sets `TypeSymbol.BclTypeName` for the three primitives:

| Keyword | BclTypeName |
|---|---|
| `number` | `"Number"` |
| `string` | `"String"` |
| `bool` | `"Bool"` |

When resolving `String.Length(s)`, the emitter checks if `String` is a `BclTypeName`, resolves the static method, and emits `global.String_Length(s)`.

A new `StaticCtorEmitter.cs` collects all static members of a type and emits the `ClassName_static_ctor` function + `gml_pragma` line. It is called by `ClassEmitter` / `StructEmitter` after emitting instance members.

### 8.5 GML Event Name Mapping

`@NativeEvent` takes a **logical event name** (e.g. `"Create"`, `"Step"`, `"Draw"`). The compiler maps it to the GML file name via a hardcoded dictionary in `GmlEventMap`:

```csharp
static class GmlEventMap
{
    private static readonly Dictionary<string, string> _map = new()
    {
        ["Create"]          = "Create_0",
        ["Destroy"]         = "Destroy_0",
        ["Step"]            = "Step_0",
        ["StepBegin"]       = "Step_1",
        ["StepEnd"]         = "Step_2",
        ["Draw"]            = "Draw_0",
        ["DrawBegin"]       = "Draw_72",
        ["DrawEnd"]         = "Draw_73",
        ["DrawGui"]         = "Draw_64",
        ["DrawGuiBegin"]    = "Draw_65",
        ["DrawGuiEnd"]      = "Draw_66",
        ["AsyncHttp"]       = "Other_62",
        ["AsyncDialog"]     = "Other_63",
        ["AsyncSaveLoad"]   = "Other_80",
        ["RoomStart"]       = "Other_4",
        ["RoomEnd"]         = "Other_5",
        ["GameStart"]       = "Other_2",
        ["GameEnd"]         = "Other_3",
        ["AnimationEnd"]    = "Other_7",
        ["Alarm"]           = "Alarm_",       // Alarm_0..Alarm_11 — suffix appended by emitter
        ["KeyPress"]        = "Keyboard_",
        ["KeyRelease"]      = "KeyRelease_",
        ["MouseLeft"]       = "Mouse_0",
        ["MouseRight"]      = "Mouse_1",
    };

    public static string Resolve(string logicalName, string? suffix = null);  // throws DiagnosticException if unknown
}
```

Unknown logical names → error `TGML0039: Unknown @NativeEvent name '{name}'.`

`FileOrganizer.GetEventPath` calls `GmlEventMap.Resolve` when writing `@Object` event files.

### 8.6 Method Overloading

Methods may be overloaded by **parameter count and/or parameter types** within the same type. Return type alone does not distinguish overloads.

**Naming in GML:** Overloaded methods get a suffix based on their parameter type list:

```
void Foo(number x)          → TypeName_Foo__number
void Foo(number x, string y) → TypeName_Foo__number_string
void Foo()                  → TypeName_Foo__
```

`NamingConvention.MethodName` accepts an optional parameter signature list and appends the suffix when the method has overload siblings. If a method has no overloads, no suffix is appended (backward-compatible, cleaner GML).

**`DuplicateMemberCheck` updated:** Rule DM02 fires only when two methods share the same name **and** identical parameter type lists (not just the same name).

**`MethodCallCheck` updated:** When resolving an invocation, the verifier selects the best-matching overload by:
1. Filter to methods with the same name.
2. Filter to those where argument count is within [required params, total params].
3. Filter to those where each argument type is assignable to the corresponding parameter type.
4. If zero candidates → error `TGML0040: No overload of '{name}' matches the supplied arguments.`
5. If multiple candidates remain → error `TGML0041: Ambiguous call to '{name}'; multiple overloads match.`

### 8.7 Emitter Matches — Non-Overlapping Design

Each `INodeEmitter.Matches` must be **mutually exclusive** with all other emitters for the same node type. There are no priorities or fallbacks — `Emitter` asserts at startup (debug only) that no two registered emitters return `true` for the same sample node type.

The `@Object` vs plain class distinction is handled entirely inside a single `ClassEmitter`. `ClassEmitter.Matches` returns `true` for all `ClassDeclarationNode`. Internally it branches:

```csharp
sealed class ClassEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var decl = (ClassDeclarationNode)node;
        if (ctx.Decorators.ObjectAssetName is not null)
            EmitGameObject(decl, ctx);
        else
            EmitScript(decl, ctx);
    }

    private void EmitGameObject(ClassDeclarationNode decl, EmitContext ctx) { ... }
    private void EmitScript(ClassDeclarationNode decl, EmitContext ctx) { ... }
}
```

This keeps `Matches` simple and non-overlapping while allowing the two code paths to differ substantially.

### 8.8 DecoratorProcessor

Runs before emitters. Reads all decorator nodes, validates them (done in Verification already), and builds a `DecoratorAnnotations` object attached to `EmitContext`:

```csharp
sealed class DecoratorAnnotations
{
    public string? ObjectAssetName { get; }           // from @Object("...")
    public string? NativeEventName { get; }           // from @NativeEvent("...")
    public string? NativePropertyName { get; }        // from @NativeProperty("...")
    public string? NativeCallName { get; }            // from @NativeCall("...")
    public string? AssetName { get; }                 // from @Asset("...")
}
```

---

## 9. Diagnostics

```csharp
record Diagnostic(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    string Message,
    SourceLocation Location
);

record SourceLocation(string FilePath, int Line, int Column);

enum DiagnosticSeverity { Error, Warning }

sealed class DiagnosticBag
{
    public void Report(DiagnosticCode code, DiagnosticSeverity severity, string message, SourceLocation location);
    public bool HasErrors { get; }
    public IReadOnlyList<Diagnostic> All { get; }
}
```

Additional error codes added by the architecture:

| Code | Phase | Description |
|---|---|---|
| TGML0039 | Verification | Unknown `@NativeEvent` logical name |
| TGML0040 | Verification | No overload of method matches supplied arguments |
| TGML0041 | Verification | Ambiguous call — multiple overloads match |
| TGML0042 | Verification | Duplicate key in Dictionary literal |
| TGML0043 | Verification | Duplicate static constructor |
| TGML0044 | Verification | Cross-class static dependency in static constructor |

Output format (for stderr):
```
path/to/file.tgml(12,5): error TGML0009: readonly field 'Name' cannot be assigned outside a constructor.
```

---

## 10. Wiring & Composition Root

All wiring happens in `Program.cs`. No dependency injection framework — explicit construction.

```csharp
// Program.cs
var options = CompilerOptions.Parse(args);
var diagnostics = new DiagnosticBag();

var bclFiles = new BclLoader(options.BclPath).GetFiles();
var userFiles = new FileScanner().Scan(options.InputPath);
var allFiles = bclFiles.Concat(userFiles);

var astBuilder = new AstBuilder(diagnostics);
var fileNodes = allFiles.Select(f => astBuilder.Build(f)).ToList();
if (diagnostics.HasErrors) { PrintAndExit(diagnostics); }

var symbolTable = new SymbolTable();
var populator = new Populator(
    new NamespacePopulator(symbolTable, diagnostics),
    new TypePopulator(symbolTable, diagnostics),
    new MemberPopulator(symbolTable, diagnostics),
    new InheritanceResolver(symbolTable, diagnostics),
    new GenericParameterBinder(symbolTable, diagnostics)
);
populator.Populate(fileNodes);
if (diagnostics.HasErrors) { PrintAndExit(diagnostics); }

var checks = new ISemanticCheck[]
{
    new TypeAssignabilityCheck(),
    new NullabilityCheck(),
    new MemberAccessCheck(),
    new MethodCallCheck(),
    new ConstructorCallCheck(),
    new OperatorCheck(),
    new ControlFlowCheck(),
    new AbstractCompletenessCheck(),
    new InterfaceImplementationCheck(),
    new OverrideSignatureCheck(),
    new SealedInheritanceCheck(),
    new StructInheritanceCheck(),
    new GenericConstraintCheck(),
    new DelegateSignatureCheck(),
    new EventAccessCheck(),
    new LambdaCheck(),
    new DecoratorPlacementCheck(),
    new ObjectDecoratorCheck(),
    new ConstExpressionCheck(),
    new ReadonlyAssignmentCheck(),
    new StaticModifierCheck(),
    new StaticConstructorCheck(),
    new WithTargetCheck(),
    new SwitchCaseConstantCheck(),
    new DuplicateCaseCheck(),
    new VarInferenceCheck(),
    new ArrayLiteralTypeCheck(),
    new ThrowTypeCheck(),
    new TypeofNameCheck(),
    new IsAsCompatibilityCheck(),
    new DuplicateMemberCheck(),
    new DuplicateParameterCheck(),
    new DefaultParameterConstCheck(),
    new ObjectCreationCheck(),
};

var verifier = new Verifier(checks, diagnostics);
verifier.Verify(fileNodes, symbolTable);
if (diagnostics.HasErrors) { PrintAndExit(diagnostics); }

var naming = new NamingConvention();
var fileOrganizer = new FileOrganizer(options.OutputPath, naming);
var writer = new GmlWriter();

var nodeEmitters = new INodeEmitter[]
{
    new ClassEmitter(), new StructEmitter(), new EnumEmitter(), new DelegateEmitter(),
    new ConstructorEmitter(), new MethodEmitter(), new PropertyEmitter(),
    new IndexerEmitter(), new OperatorEmitter(), new FieldEmitter(), new EventEmitter(),
    // statements
    new BlockStatementEmitter(), new IfStatementEmitter(), new WhileStatementEmitter(),
    new ForStatementEmitter(), new RepeatStatementEmitter(), new SwitchStatementEmitter(),
    new WithStatementEmitter(), new TryStatementEmitter(), new ReturnStatementEmitter(),
    new ThrowStatementEmitter(), new VarDeclarationStatementEmitter(),
    new ExpressionStatementEmitter(),
    // expressions
    new BinaryExpressionEmitter(), new UnaryExpressionEmitter(), new TernaryExpressionEmitter(),
    new AssignmentExpressionEmitter(), new MemberAccessExpressionEmitter(),
    new InvocationExpressionEmitter(), new ObjectCreationExpressionEmitter(),
    new LambdaExpressionEmitter(), new NullCoalescingExpressionEmitter(),
    new NullConditionalExpressionEmitter(), new ArrayLiteralExpressionEmitter(),
    new TypeofExpressionEmitter(), new NameofExpressionEmitter(),
    new DefaultExpressionEmitter(),
};

var emitter = new Emitter(nodeEmitters, new DecoratorProcessor(), fileOrganizer, symbolTable);
emitter.Emit(fileNodes);

PrintDiagnostics(diagnostics);
```

Adding a new check = create `MyNewCheck.cs` implementing `ISemanticCheck`, add one line to the array above. No other files change.

Adding a new emitter = create `MyNewEmitter.cs` implementing `INodeEmitter`, add one line to the array above. No other files change.
