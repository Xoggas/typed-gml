# AGENTS.md — TypedGML Compiler

## What you are building

A transpiler from **TypedGML** (`.tgml`) to **GML** (GameMaker Language).  
Implementation language: **C# / .NET 10.0**.  
Parser and lexer: **ANTLR4** (`Antlr4.Runtime.Standard` NuGet package).

The two authoritative documents for this project are:

- `TypedGML_Specification.md` — the language spec. Every type system rule, operator, statement, decorator, and code generation rule lives here. When in doubt about language behaviour, this file is the answer.
- `TypedGML_Architecture.md` — the compiler architecture. Project structure, interfaces, phases, semantic checks, emitters, naming conventions, and wiring. When in doubt about where code goes or how classes relate, this file is the answer.

Read both documents fully before writing any code.

---

## Non-negotiable rules

These rules apply to every file you write, every PR, every fix. No exceptions.

### File and class size

- No class longer than **150 lines**. Hard limit: 200 lines.
- If a class is growing past 150 lines, split it by responsibility before continuing.
- One public type per file. File name matches type name exactly.

### No documentation comments

- No `///` XML doc comments anywhere in implementation code.
- No `//` block comments explaining what a method does.
- Readability comes from naming, not prose. If a name needs a comment to be understood, rename it.
- Inline comments are allowed **only** for non-obvious GML-specific workarounds or ANTLR quirks.

### Naming

- Follow the naming conventions already established in `NamingConvention.cs`. Do not invent GML names anywhere else.
- C# naming: PascalCase for types and public members, camelCase for locals and parameters, `_camelCase` for private fields.
- Names must be self-explanatory. `ResolveBaseType`, `EmitPropertyGetter`, `CheckReadonlyAssignment` — not `Process`, `Handle`, `Do`.

### SOLID

- **SRP:** Every class has one reason to change. `ClassEmitter` emits classes. `NamingConvention` derives names. `DuplicateMemberCheck` checks for duplicate members. Nothing does two jobs.
- **OCP:** Adding a new semantic check = one new file implementing `ISemanticCheck` + one line in `Program.cs`. Adding a new emitter = one new file implementing `INodeEmitter` + one line in `Program.cs`. No existing files change.
- **LSP:** Every `ISemanticCheck` implementation must be safely substitutable. `Matches` must be honest — if it returns `true`, `Check` must handle that node without casting errors.
- **ISP:** Do not add methods to `ISemanticCheck`, `INodeEmitter`, or any other interface without discussion. Keep interfaces at ≤5 members.
- **DIP:** Orchestrators (`Verifier`, `Emitter`, `Populator`, `CompilerPipeline`) depend on interfaces and abstract lists, never on concrete check or emitter classes directly.

### No static state

- No mutable static fields anywhere.
- `NamingConvention`, `GmlEventMap`, `TypeHelper`, `PrimitiveOperationRegistry` may be static classes with pure functions and immutable static data (readonly dictionaries initialized once).
- Everything else is instantiated and injected.

### Errors never throw

- Semantic and syntactic errors go through `DiagnosticBag.Report(...)`. Never `throw` for a user-facing error.
- `throw` is reserved for genuine programmer invariant violations (e.g. a `switch` arm that should be unreachable).
- After any phase that produces errors, check `DiagnosticBag.HasErrors` and halt before proceeding.

### Immutable AST

- All AST node types are `record` types. Do not add mutable state to AST nodes.
- AST nodes are never modified after construction. Passes that need to annotate nodes use separate data structures (e.g. `DecoratorAnnotations`, `SymbolTable`).

---

## Phase order

Always implement and verify in this order. Do not start Emission until Verification passes on a meaningful set of test inputs.

1. **AST node definitions** (`Ast/`) — all records, no logic.
2. **`AstBuilder`** — ANTLR visitor → AST. Wire `ParseErrorListener` to `DiagnosticBag`.
3. **`SymbolTable` + `TypeSymbol` + `MemberSymbol`** — data structures only.
4. **Population phase** — `NamespacePopulator` → `TypePopulator` → `MemberPopulator` → `InheritanceResolver` → `GenericParameterBinder`, in that order.
5. **Verification phase** — implement checks one category at a time. Start with: type assignability, member access, control flow. Then add the rest.
6. **Emission phase** — implement emitters bottom-up: expressions first, then statements, then members, then types.
7. **BCL** — integrate BCL `.tgml` files last, after the core pipeline works end-to-end on a minimal test case.

---

## Semantic checks

Every check lives in `Verification/Checks/` and implements `ISemanticCheck`.

```csharp
interface ISemanticCheck
{
    bool Matches(IAstNode node);
    void Check(IAstNode node, VerificationContext ctx);
}
```

Rules:
- `Matches` must be a simple type check (`node is SomeNode`) or a type check plus a flag (`node is AssignmentExpressionNode a && a.Operator == "="`). No heavy logic in `Matches`.
- `Check` receives the node already cast-safe (cast it immediately at the top).
- All errors reported via `ctx.Diagnostics.Report(...)` with the correct `DiagnosticCode`, `SourceLocation`, and a clear English message.
- A check must never modify the AST or the `SymbolTable`.
- A check may read and update `VerificationContext` flags (`IsInLoop`, `IsInConstructor`, etc.) when it enters/exits a scope — but must restore them before returning.

The full list of required checks and their rules is in `TypedGML_Architecture.md §7`. Implement every check listed there. Do not skip any.

---

## Emitters

Every emitter lives in `Emission/Emitters/` (or its `Statements/` / `Expressions/` subdirectory) and implements `INodeEmitter`.

```csharp
interface INodeEmitter
{
    bool Matches(IAstNode node);
    void Emit(IAstNode node, EmitContext ctx);
}
```

Rules:
- `Matches` implementations must be non-overlapping across all registered emitters. `ClassEmitter` handles all `ClassDeclarationNode` — the `@Object` vs script branch is an internal `if`, not a second emitter.
- Emitters do not resolve types. All type information is already in `SymbolTable`. Read it; do not recompute it.
- All GML name derivation goes through `ctx.Naming` (which is `NamingConvention`). Never concatenate GML names manually in an emitter.
- Write GML output through `ctx.Writer` only. Do not write to disk directly from an emitter.
- `FileOrganizer` decides where output goes. Call `ctx.Files.GetScriptPath(type)` or `ctx.Files.GetEventPath(type, eventName)` — never hardcode paths.

---

## GML output expectations

- GML uses `var` for local variable declarations.
- GML functions are declared as `function name(params) { }` at the global scope.
- GML struct literals use `{ field: value }`.
- `#macro NAME value` is used for `const` fields and `enum` members.
- `global.name` is used for `global`-modified properties.
- `ds_map_create()` / `ds_map_add()` / `ds_map_destroy()` are used for `Dictionary<K,V>` literals and BCL methods.
- Dictionary brace literals `{"key": value}` emit as an IIFE wrapping `ds_map_create` + `ds_map_add` calls — see `TypedGML_Specification.md §3.2`.
- Brace literals are only valid as the RHS of a `Dictionary<K,V>` variable. Any other target type is a compile error.
- Do not emit TypeScript, do not emit C#-style syntax. When unsure what valid GML looks like for a construct, check `TypedGML_Specification.md §19`.

---

## BCL integration

- BCL `.tgml` files are located in a `bcl/` folder alongside the compiled executable.
- `BclLoader` finds this folder via `Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)`.
- BCL files are prepended to the file list before user files. Population processes them first.
- Primitive types (`number`, `string`, `bool`, `void`, `null`, `object`) are registered in `BuiltinTypeRegistry` in C# code — they do not come from BCL files.
- Primitive operator rules are in `PrimitiveOperationRegistry` in C# code.
- `Exception` is defined in a BCL `.tgml` file but is also known to the compiler by name for `throw`/`catch` checks.

---

## Method overloading

- Methods may share a name if their parameter type lists differ.
- GML name suffix: `TypeName_MethodName__TypeA_TypeB`. If a method has no overload siblings, no suffix is appended.
- Overload resolution in `MethodCallCheck`: filter by name → filter by argument count within [required, total] → filter by assignable types → error if zero or multiple candidates remain.
- `DuplicateMemberCheck` only fires when two methods share name **and** identical parameter type lists.

---

## GML event mapping

`@NativeEvent` takes a logical name. The mapping to GML file names lives entirely in `GmlEventMap`. If a logical name is not in the map, emit `TGML0035`. Do not let the string pass through unvalidated.

---

## Project layout

Follow the directory structure in `TypedGML_Architecture.md §2` exactly. Do not reorganize it. Do not add new top-level folders without a clear reason.

---

## What not to do

- Do not add a dependency injection framework. Wiring is explicit in `Program.cs`.
- Do not use `dynamic` or reflection-based dispatch for checks or emitters.
- Do not share mutable state between checks. Each check is self-contained.
- Do not write integration logic inside an emitter. Emitters emit; they do not coordinate other emitters.
- Do not add `static` mutable fields to any class.
- Do not silently swallow errors. Every error path must call `ctx.Diagnostics.Report`.
- Do not generate GML that is not valid GML 2.3+.
- Do not add features not described in `TypedGML_Specification.md`. If something is unclear, re-read the spec before inventing behaviour.