# TypedGML Language Specification
**Version:** 1.0 (pre-implementation)  
**Implementation language:** C#  
**Parser/Lexer:** ANTLR4  
**Target language:** GML (GameMaker Language, runtime 2.3+)  
**File extension:** `.tgml`  
**Compiler phases:** Population → Verification → Emission

---

## Table of Contents
1. [Primitive Types](#1-primitive-types)
2. [Nullable Types](#2-nullable-types)
3. [Arrays and Collections](#3-arrays-and-collections)
4. [Operators and Expressions](#4-operators-and-expressions)
5. [Statements](#5-statements)
6. [Functions and Parameters](#6-functions-and-parameters)
7. [Enums](#7-enums)
8. [Classes](#8-classes)
9. [Structs](#9-structs)
10. [Interfaces](#10-interfaces)
11. [Delegates and Events](#11-delegates-and-events)
12. [Lambdas](#12-lambdas)
13. [Generics](#13-generics)
14. [Decorators](#14-decorators)
15. [Namespaces and Using](#15-namespaces-and-using)
16. [Access Modifiers and Other Modifiers](#16-access-modifiers-and-other-modifiers)
17. [XML Doc Comments](#17-xml-doc-comments)
18. [Runtime Model and Type Information](#18-runtime-model-and-type-information)
19. [Code Generation Rules](#19-code-generation-rules)
20. [BCL (Base Class Library)](#20-bcl-base-class-library)
21. [Compiler Architecture](#21-compiler-architecture)
22. [Error Catalogue](#22-error-catalogue)

---

## 1. Primitive Types

| TypedGML type | GML equivalent | Notes |
|---|---|---|
| `number` | `real` | Only numeric type. No `int`/`float` aliases. |
| `string` | `string` | Only `"..."` literals. No verbatim/interpolated strings. |
| `bool` | `bool` | `true`/`false` literals only. No implicit conversion with `number`. |
| `void` | — | Return type only; not a value type. |
| `null` | `undefined` | The null literal is `null`. Maps to GML `undefined`. |
| `object` | `struct` (GML) | Base type for all classes and structs. Has one method: `ToString() → string`. |

**Key rules:**
- `number` and `bool` are distinct types. `0` is not `false`. Assigning one to the other is a compile error.
- `null` is assignable to any nullable type (see §2) and any reference type (class, struct, interface, delegate).
- `object` is the implicit base of every class and struct if no explicit base is declared.

---

## 2. Nullable Types

Any type can be made nullable with `?`:

```tgml
number? x = null;
string? name = null;
MyClass? obj = null;
```

**Operators:**
- `??` — null coalescing: `x ?? defaultValue`
- `?.` — null conditional member access: `obj?.Field`, `obj?.Method()`
- `?.[]` — null conditional indexer: `arr?.[i]`

**Rules:**
- `T` and `T?` are distinct types at compile time.
- Assigning `T?` to `T` without unwrapping is a compile error.
- `null` cannot be assigned to a non-nullable type.
- Nullable primitives (`number?`, `bool?`) emit GML `undefined` checks at call sites.

**GML emission for null check:**
```gml
// obj?.Method() emits:
(obj != undefined ? obj.Method() : undefined)
```

---

## 3. Arrays and Collections

### 3.1 Static Arrays

TypedGML arrays are **jagged** (arrays of arrays): `T[]`, `T[][]`, `T[][][]`, etc.

**Syntax:**
```tgml
number[] arr = [1, 2, 3];
string[][] matrix = [["a","b"], ["c","d"]];
```

- **Literal-only initialization.** No `new T[n]` syntax.
- Arrays are **fixed-size** from the TypedGML perspective (no push/pop on raw arrays).
- Emit directly as GML arrays.
- Element access: `arr[i]` (zero-indexed, as in GML).
- No built-in `.length` on raw arrays — use BCL `List<T>` instead.

### 3.2 BCL List

`List<T>` is a BCL type wrapping a GML array:
```tgml
List<number> nums = [1, 2, 3];  // literal initializes the list
nums.Add(4);
nums.Remove(1);
number len = nums.Count;
```

Literal `[...]` can initialize either `T[]` or `List<T>` — the type is inferred from the variable declaration.

---

## 4. Operators and Expressions

### 4.1 Arithmetic

| Operator | Return type | Notes |
|---|---|---|
| `+`, `-`, `*`, `/`, `%` | `number` | Both operands must be `number`. |
| `+` (string) | `string` | If either operand is `string`, performs concatenation. |
| `-` (unary) | `number` | Negation. |
| `~` (unary) | `number` | Bitwise NOT. |

### 4.2 Logical Operators

> **No `&&`, `||`, `!`.** TypedGML uses keyword operators only.

| Operator | Meaning | Operands | Return |
|---|---|---|---|
| `and` | Logical AND | `bool` × `bool` | `bool` |
| `or` | Logical OR | `bool` × `bool` | `bool` |
| `not` | Logical NOT | `bool` | `bool` |

### 4.3 Comparison

All comparison operators return `bool`.

| Operator | Notes |
|---|---|
| `==`, `!=` | Structural equality for primitives; reference equality for objects unless `==` is overloaded. |
| `<`, `>`, `<=`, `>=` | `number` only unless overloaded. |

### 4.4 Null Operators

| Operator | Syntax | Notes |
|---|---|---|
| Null coalescing | `a ?? b` | Returns `a` if not null, else `b`. |
| Null conditional | `a?.b` | Returns `a.b` if `a` not null, else `null`. |
| Null conditional call | `a?.Method()` | Calls if not null, else `null`. |

### 4.5 Assignment Operators

`=`, `+=`, `-=`, `*=`, `/=`, `%=`

Also for delegates: `+=`, `-=` (subscribe/unsubscribe).

### 4.6 Ternary

```tgml
result = condition ? valueIfTrue : valueIfFalse;
```

### 4.7 Type Operators

| Operator | Syntax | Notes |
|---|---|---|
| `is` | `expr is TypeName` | **Compile-time only.** Returns `bool`. Used for LSP and type narrowing. No GML emission; resolved at compile time. |
| `as` | `expr as TypeName` | **Compile-time cast.** Informs the compiler that the expression is of type `TypeName`. No runtime check emitted. Unsafe — if wrong, GML will error naturally. |
| `typeof` | `typeof(TypeName)` | Returns a `string` — the fully qualified type name. Emits a GML string literal at the call site. |
| `nameof` | `nameof(x)`, `nameof(x.y.z)` | Returns a `string` — the last identifier in the chain. Emits a GML string literal. |
| `default` | `default(TypeName)` | Returns the default value for the type (see below). |

**`default(T)` values:**

| Type | Default |
|---|---|
| `number` | `0` |
| `bool` | `false` |
| `string` | `undefined` |
| class | `undefined` |
| struct | New instance with all fields recursively defaulted down to primitives |
| `T[]` / `List<T>` | `undefined` |
| `T?` (nullable) | `null` |

**`default(T)` in generics:** Emits a helper call `__tgml_default(typeArg)` that branches on the `__genericArgs` entry.

### 4.8 Operator Overloading

Supported in classes and structs. Syntax mirrors C#:

```tgml
public static MyType operator +(MyType a, MyType b) { ... }
public static explicit operator number(MyType a) { ... }
public static implicit operator MyType(number n) { ... }
```

**Overloadable:** `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `~`, unary `-`  
**Conversion operators:** `implicit`, `explicit`  
**Not overloadable:** `and`, `or`, `not`, `=`, `?.`, `??`

---

## 5. Statements

### 5.1 Variable Declaration

```tgml
number x = 5;
var y = "hello";          // type inferred from RHS
number? z = null;
```

`var` triggers type inference. The inferred type is fixed at compile time. `var` is disallowed if the RHS is `null` (ambiguous type) unless an explicit cast or type annotation is provided.

### 5.2 If / Else

```tgml
if (condition) {
    ...
} else if (otherCondition) {
    ...
} else {
    ...
}
```

Condition must be `bool` (no implicit `number` → `bool`).

### 5.3 While

```tgml
while (condition) { ... }
```

### 5.4 For

```tgml
for (var i = 0; i < 10; i += 1) { ... }
```

Init, condition, update — all optional. Condition must be `bool`.

### 5.5 Repeat

```tgml
repeat (n) { ... }
```

`n` must be `number`. Emits GML `repeat(n)`.

### 5.6 Switch

Value-based only. Case labels must be **compile-time constants** (`const` fields, enum members, or literals).

```tgml
switch (value) {
    case 1:
        ...
        break;
    case MyEnum.Foo:
        ...
        break;
    default:
        ...
        break;
}
```

No fall-through without explicit `break`. No pattern matching.

### 5.7 With

GML-specific. Restricted to instances of `@Object`-decorated classes only. Using `with` on a struct is a **compile error**.

```tgml
with (instanceExpr) {
    someField = 10;
}
```

Emits GML `with (expr) { ... }`.

### 5.8 Return / Break / Continue

```tgml
return value;
return;          // void methods
break;
continue;
```

### 5.9 Try / Catch / Finally

Uses GML 2.3+ native try/catch. `catch` captures a single `Exception` variable.

```tgml
try {
    ...
} catch (Exception e) {
    var msg = e.message;
} finally {
    ...
}
```

`throw` is a **statement only**:

```tgml
throw new Exception("Something went wrong");
```

`Exception` is a BCL struct (see §20). Only `Exception` can be thrown/caught. Catching an arbitrary GML value is not supported.

---

## 6. Functions and Parameters

### 6.1 Top-Level Functions

Functions declared outside any class/struct emit as global GML scripts.

```tgml
public number Add(number a, number b) {
    return a + b;
}
```

### 6.2 Default Parameters

```tgml
void Greet(string name, number times = 1) { ... }

Greet("Alice");          // times = 1
Greet("Bob", 3);
```

Default values must be compile-time constants or literals.

### 6.3 Named Arguments

```tgml
void Foo(number x, number y, number z = 0) { ... }

Foo(y: 2, x: 1);        // z defaults to 0
Foo(x: 1, y: 2, z: 3);
```

Named and positional arguments can be mixed. Positional arguments must come before named ones.

### 6.4 No Support

- No `params T[]` variadic parameters.
- No `ref` / `out` parameters.
- No extension methods.

---

## 7. Enums

Number-based only. No string enums, no `[Flags]`.

```tgml
public enum Direction {
    North = 0,
    South = 1,
    East  = 2,
    West  = 3
}
```

- All values must be **explicit integer number literals**.
- If a value is omitted, it is the previous value + 1 (starting at 0).
- Enum members emit as GML `#macro` constants: `#macro Direction_North 0`.
- `typeof(Direction)` → `"Direction"`.
- `nameof(Direction.North)` → `"North"`.
- Enum values are of type `number` in GML; the compiler enforces `Direction` type at assignment sites.

---

## 8. Classes

### 8.1 Declaration

```tgml
[modifiers] class ClassName [: BaseClass] [, IInterface1, IInterface2] {
    ...
}
```

- Single inheritance only.
- Multiple interface implementation.
- Implicit base is `object` if no base class given.

### 8.2 Fields

```tgml
private number _x;
public readonly string Name;
public const number MaxCount = 100;
```

- `readonly` — compile-time enforcement; can only be assigned in the constructor body.
- `const` — emits as a GML `#macro`. Must be assigned a compile-time constant expression.
- `global` is **not** a field modifier; it applies to **properties** only (see §8.4).

### 8.3 Constructors

```tgml
public MyClass(number x, number y) : base(x) {
    _x = x;
    _y = y;
}

public MyClass(number x) : this(x, 0) { }  // constructor chaining
```

- `base(...)` calls the parent constructor.
- `this(...)` delegates to another constructor in the same class.
- `base(...)` / `this(...)` executes before the constructor body.

### 8.4 Properties

Auto-property:
```tgml
public number X { get; set; }
public string Name { get; }           // read-only auto-property (set only in constructor)
```

Property with body:
```tgml
private number _x;
public number X {
    get { return _x; }
    set { _x = value; }
}
```

- `value` — the implicit parameter in `set`.
- `field` — refers to the compiler-generated backing field of an auto-property (usable inside the same property body only).
- `global` modifier — makes the property emit as `global.PropertyName` in GML:

```tgml
public global number Score { get; set; }
// emits: global.Score in all get/set contexts
```

`global` properties are only valid at class/struct scope. They become GML global variables.

Abstract property:
```tgml
public abstract number Value { get; set; }
```

### 8.5 Methods

```tgml
public number Compute(number input) {
    return input * 2;
}
```

Virtual/override:
```tgml
public virtual string ToString() {
    return "MyClass";
}

// In child:
public override string ToString() {
    return "Child: " + base.ToString();
}
```

**Virtual dispatch strategy:** The compiler **inlines the method body** of the base into the child for all `override` methods. `base.Method()` calls are resolved at compile time by inserting the parent's body inline (with appropriate renaming). No vtable at runtime.

Abstract:
```tgml
public abstract number Calculate(number x);
```

### 8.6 Indexers

```tgml
public number this[number index] {
    get { return _items[index]; }
    set { _items[index] = value; }
}
```

Multiple indexers with different index types are allowed.
Read-only indexers: omit `set`.

### 8.7 new — Object Instantiation

**Struct:**
```tgml
var s = new MyStruct(1, 2);
// emits: MyStruct_create(1, 2) — a GML script returning a struct
```

**@Object class (GameObject):**
```tgml
var inst = new MyObject(x, y, layer);
// emits: MyObject_create(x, y, layer)
```

The `MyObject_create` script:
1. Calls `instance_create_layer(x, y, layer, "OBJ_MyObject")` using the `@Object("OBJ_MyObject")` binding.
2. If the class has additional constructor parameters (beyond the three spatial ones), sets them via a `with` block on the returned instance.
3. If no additional parameters: directly returns the instance without a `with` block.

### 8.8 Abstract Classes

- Cannot be instantiated. `new AbstractClass(...)` is a **compile error**.
- All `abstract` members must be overridden in concrete subclasses. Failure to do so is a **compile error**.

### 8.9 Sealed Classes

- Cannot be subclassed. Inheriting from a `sealed` class is a **compile error**.

### 8.10 object Base Type

`object` is the root of all classes and structs. It provides:
```tgml
public virtual string ToString() { return "<object>"; }
```

---

## 9. Structs

```tgml
public struct Point {
    public number X;
    public number Y;

    public Point(number x, number y) {
        X = x;
        Y = y;
    }

    public string ToString() { return "(" + X + ", " + Y + ")"; }
}
```

- All structs are **reference types** in GML (GML structs are always by reference).
- Structs can implement interfaces.
- Structs **cannot inherit** from other structs or classes (only from `object` implicitly).
- Structs support: fields, properties, methods, constructors, indexers, operator overloading.
- Structs **cannot** be `abstract` or `sealed`.
- `default(MyStruct)` recursively creates a struct with all fields at their type defaults.

---

## 10. Interfaces

```tgml
public interface IShape {
    number Area { get; }
    string Describe();
    number this[number index] { get; }
    event Action<number> OnChange;
}
```

- Can contain: **method signatures**, **property signatures**, **indexer signatures**, **event declarations**.
- No default implementations.
- No explicit interface implementation (`IFoo.Bar()` style is not supported).
- All interface members are implicitly `public`.
- A class that implements an interface but does not implement all members is a **compile error**.
- Interfaces can extend other interfaces: `interface IFoo : IBar, IBaz { ... }`.

---

## 11. Delegates and Events

### 11.1 Delegate Declaration

```tgml
public delegate number Transformer(number input);
public delegate void Callback(string msg, bool success);
```

Delegates are **multicast**: they hold an array of functions. Calling a delegate calls all subscribers in order. The return value is the result of the **last** subscriber (undefined if no subscribers).

**GML emission:** A delegate variable is a GML array of method references.

```gml
// Declaration var
var _myDelegate = [];

// Subscribe
_myDelegate[array_length(_myDelegate)] = someFunction;

// Unsubscribe
// (compiler emits array-filter helper)

// Invoke
__tgml_invoke_delegate(_myDelegate, arg0, arg1);
```

### 11.2 Subscribe / Unsubscribe

```tgml
myDelegate += HandlerMethod;
myDelegate -= HandlerMethod;
```

### 11.3 Built-in Delegate Types

Defined in BCL. All are generic.

```tgml
Action                           // () → void
Action<T>                        // (T) → void
Action<T1, T2>                   // (T1, T2) → void
// ... up to Action<T1..T8>

Func<TResult>                    // () → TResult
Func<T, TResult>                 // (T) → TResult
Func<T1, T2, TResult>            // (T1, T2) → TResult
// ... up to Func<T1..T8, TResult>
```

### 11.4 Events

Events are declared inside classes and interfaces. An `event` wraps a delegate and restricts direct assignment from outside the declaring class (only `+=`/`-=` are public).

```tgml
public event Action<number> OnScoreChanged;
```

- Inside the declaring class: can assign (`= null`), invoke, `+=`, `-=`.
- Outside the declaring class: only `+=` and `-=`.
- Emits the same delegate array pattern, but with access modifier enforcement at compile time.

---

## 12. Lambdas

```tgml
var fn = (number x) => x * 2;
var fn2 = (number x, number y) => {
    return x + y;
};
```

- No closure capture. Lambdas cannot reference variables from the enclosing scope (compile error if attempted). Parameters and their own local variables only.
- The lambda type is inferred from context (assignment target delegate type, parameter type, etc.).
- No discard parameter `_`.
- Emits as an anonymous GML `function(...)` expression.

---

## 13. Generics

### 13.1 Generic Types

```tgml
public class Container<T> {
    private T _value;
    public Container(T value) { _value = value; }
    public T Get() { return _value; }
}
```

### 13.2 Multiple Type Parameters

```tgml
public class Pair<T, U> {
    public T First;
    public U Second;
}
```

### 13.3 Constraints

```tgml
public class Foo<T : IComparable> { ... }            // interface constraint
public class Bar<T : BaseClass> { ... }              // class constraint
public class Baz<T : MyStruct> { ... }               // struct constraint
public class Multi<T : IFoo, U : IBar> { ... }       // multiple parameters with constraints
```

- Supported constraints: **interface names**, **class names**, **struct names**.
- **Not supported:** `T : class`, `T : new()`, `T : struct` (keyword constraints).
- Multiple constraints on one parameter: **not supported** (pick the most specific type).

### 13.4 Generic Methods

```tgml
public T Identity<T>(T value) {
    return value;
}

public void Swap<T>(T[] arr, number i, number j) {
    var tmp = arr[i];
    arr[i] = arr[j];
    arr[j] = tmp;
}
```

### 13.5 Generic Interfaces and Delegates

```tgml
public interface IRepository<T> {
    T GetById(number id);
    void Save(T item);
}

public delegate TResult Converter<T, TResult>(T input);
```

### 13.6 Runtime Representation

- **Type erasure:** Generics are erased at runtime. No separate GML code per instantiation.
- **`__genericArgs`:** A struct field present on each generic class instance, storing the runtime string names of type arguments.

```gml
// new Container<number>()  emits:
var inst = Container_create();
inst.__genericArgs = { T: "number" };
```

- `default(T)` in generic context emits `__tgml_default(__genericArgs.T)`.
- `typeof(T)` in generic context reads from `__genericArgs.T`.

---

## 14. Decorators

### 14.1 Placement

Decorators may appear on:
- **Classes**
- **Methods**
- **Properties**

Multiple decorators on a single element are allowed.

### 14.2 Built-in Decorators

All built-in decorators affect **code generation**. They are defined in BCL but recognized and processed by the compiler.

| Decorator | Target | Effect |
|---|---|---|
| `@Object("gmlName")` | class | Binds the class to a GML object asset. `new T(x,y,layer)` calls `instance_create_layer`. |
| `@NativeEvent("eventName")` | method | Binds this method to a GML event (e.g. `"Create"`, `"Step"`, `"Draw"`). Emits to the corresponding GML event file. |
| `@NativeProperty("gmlName")` | property | Binds the property to a GML native property (e.g. `"x"`, `"y"`, `"speed"`). `get` emits as `gmlName`, `set` as `gmlName = value`. |
| `@NativeCall("funcName")` | method | Body is ignored. Emits a direct call to GML function `funcName(...)`. |
| `@Asset("assetName")` | property | Binds to a GML asset by name. Get emits the asset reference. |

### 14.3 User-Defined Decorators

User-defined decorators may be declared in BCL files. They are **syntactically valid** and parsed, but the compiler performs **no special processing** on them — they are metadata annotations only (useful for LSP tooling, future extensions).

```tgml
// In BCL:
public decorator MyCustomDecorator(string param) { }

// Usage (valid, no compilation effect):
@MyCustomDecorator("hello")
public class Foo { }
```

---

## 15. Namespaces and Using

### 15.1 Namespace Declaration

```tgml
namespace MyGame.Entities {
    public class Player { ... }
}
```

- Dot-separated namespace segments become underscore-separated prefixes in GML.
- `MyGame.Entities.Player` → GML identifier prefix `MyGame_Entities_Player_`.

### 15.2 Using Directive

```tgml
using MyGame.Entities;             // brings all names from namespace into scope
using P = MyGame.Entities.Player;  // type alias
```

- `using` is file-scoped.
- Alias form allows `P` to be used in place of the full name.

### 15.3 File-Scoped Namespaces

A file may declare one namespace at the top without braces (applies to the whole file):

```tgml
namespace MyGame.UI;

public class HUDController { ... }
```

---

## 16. Access Modifiers and Other Modifiers

### 16.1 Access Modifiers

All access control is **compile-time only**. In emitted GML, all functions are global.

| Modifier | Meaning |
|---|---|
| `public` | Accessible from anywhere. |
| `protected` | Accessible within the declaring class and subclasses. |
| `private` | Accessible only within the declaring class. |

Default (no modifier): `private` for members, `public` for top-level declarations.

### 16.2 Other Modifiers

| Modifier | Applies to | Meaning |
|---|---|---|
| `static` | — | **Not supported.** Use `global` properties for global state. |
| `abstract` | class, method, property | Class cannot be instantiated; members must be overridden. |
| `sealed` | class | Cannot be subclassed. |
| `virtual` | method, property | Can be overridden in subclasses. |
| `override` | method, property | Overrides a `virtual` or `abstract` member. |
| `readonly` | field | Can only be assigned in the constructor. Compile-time check. |
| `const` | field | Compile-time constant. Emits as GML `#macro`. |
| `global` | property | Emits as `global.PropertyName` in GML. |

---

## 17. XML Doc Comments

Single-line XML doc comments use `///`:

```tgml
/// <summary>Calculates the area of the shape.</summary>
/// <param name="scale">A scale multiplier.</param>
/// <returns>The computed area as a number.</returns>
public number GetArea(number scale) { ... }
```

Supported tags: `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<example>`, `<exception>`, `<typeparam>`.

The compiler **collects** doc comments and can optionally emit a documentation artifact (e.g. JSON side-file) alongside the GML output. They do not affect code generation.

---

## 18. Runtime Model and Type Information

### 18.1 No `__types` Table

TypedGML does **not** emit a global `__types` runtime table. All type information is resolved at compile time.

### 18.2 GetType()

`GetType()` is a method on `object` (and thus available on all instances). It returns a `string`.

```tgml
var t = myObj.GetType();  // returns e.g. "MyGame_Entities_Player"
```

Emits: the compiler substitutes a string literal at the call site — no runtime lookup.

### 18.3 is / as

Both are **compile-time only**:

- `expr is TypeName` → evaluates to `true` or `false` at compile time based on known types. The compiler may use this for narrowing in LSP/flow analysis. Emits `true` or `false` literal if resolvable, or a GML variable comparison otherwise.
- `expr as TypeName` → an unsafe cast annotation. The compiler trusts it and adjusts the known type of the expression. **No runtime check is emitted.**

### 18.4 Generics and __genericArgs

Each generic class instance has a `__genericArgs` struct field set at construction time:

```gml
inst.__genericArgs = { T: "number", U: "string" };
```

This enables `typeof(T)` → `inst.__genericArgs.T` inside generic methods.

---

## 19. Code Generation Rules

### 19.1 Output File Structure

| Source construct | Output |
|---|---|
| Non-`@Object` class / struct | One `.gml` script file per class/struct. |
| `@Object`-decorated class | One `.gml` file **per GML event** (Create, Step, Draw, etc.), named `OBJ_Name_EventName.gml`. Initialization code goes in the Create event. |
| Top-level functions | Emitted into a shared scripts file for the namespace, or one file per function. |
| `const` fields | `#macro ClassName_FieldName value` emitted at the top of the class's output file or a shared macros file. |
| `enum` members | `#macro EnumName_MemberName value` emitted into a dedicated enums file. |

### 19.2 Naming Convention

| TypedGML | GML |
|---|---|
| `MyClass` (no namespace) | `MyClass` |
| `MyGame.Entities.Player` | `MyGame_Entities_Player` |
| Method `Foo.Bar()` | `Foo_Bar` |
| Constructor for `Foo` (struct) | `Foo_create` |
| Constructor for `@Object Foo` | `Foo_create` (instance_create_layer wrapper) |
| Property getter `Foo.X` | `Foo_get_X` |
| Property setter `Foo.X` | `Foo_set_X` |
| Operator `+` on `Foo` | `Foo_op_add` |
| Implicit conversion on `Foo` | `Foo_op_implicit_from_number` |
| Explicit conversion on `Foo` | `Foo_op_explicit_to_number` |

Namespace collisions are resolved by the full prefix; the compiler errors on any remaining collision.

### 19.3 Virtual Dispatch

No runtime vtable. When a method is `override`:
1. The compiler copies the resolved method body (from the nearest ancestor that defines it) into the child class's output function.
2. Any `base.Method()` call inside an `override` method is replaced inline with the parent's body (with renamed locals to avoid collision).
3. The child's GML function is standalone and does not call the parent's GML function.

### 19.4 Access Modifier Enforcement

All access checks are performed during the **Verification phase**. No access metadata is emitted into GML.

### 19.5 const Emission

```tgml
public const number MaxSpeed = 100;
// emits:
#macro MyClass_MaxSpeed 100
```

Usage: `MyClass.MaxSpeed` in TypedGML → `MyClass_MaxSpeed` in GML.

### 19.6 global Property Emission

```tgml
public global number Score { get; set; }
// getter emits: global.Score
// setter emits: global.Score = value
```

### 19.7 @Object Constructor Emission

```tgml
@Object("OBJ_Player")
public class Player {
    public number Health;
    public Player(number x, number y, string layer, number health) { ... }
}

// new Player(100, 200, "Instances", 50) emits:
var __inst = instance_create_layer(100, 200, "Instances", OBJ_Player);
with (__inst) {
    Health = 50;
}
return __inst;
```

If there are no class-specific constructor parameters (only x, y, layer):
```gml
return instance_create_layer(x, y, layer, OBJ_Player);
```

### 19.8 Delegate Emission

```tgml
myDelegate += Handler;
// emits:
myDelegate[array_length(myDelegate)] = Handler;

myDelegate -= Handler;
// emits (using BCL helper):
myDelegate = __tgml_delegate_remove(myDelegate, Handler);

myDelegate(arg1, arg2);
// emits:
__tgml_invoke_delegate(myDelegate, arg1, arg2);
```

### 19.9 Nullable Emission

```tgml
obj?.Field
// emits:
(obj != undefined ? obj.Field : undefined)

a ?? b
// emits:
(a != undefined ? a : b)
```

---

## 20. BCL (Base Class Library)

The BCL is written in TypedGML itself (with `@NativeCall` where needed) and compiled as part of every project.

### 20.1 Exception

```tgml
public struct Exception {
    public string message;
    public string stackTrace;
    public Exception? innerException;

    public Exception(string message) { ... }
    public Exception(string message, Exception inner) { ... }
}
```

Only `Exception` is throwable. Emits GML struct that native GML `try/catch` can catch.

### 20.2 Collections

```tgml
public class List<T> {
    public number Count { get; }
    public T this[number index] { get; set; }
    public void Add(T item) { ... }
    public void Remove(T item) { ... }
    public void RemoveAt(number index) { ... }
    public void Clear() { ... }
    public bool Contains(T item) { ... }
    public T[] ToArray() { ... }
}

public class Dictionary<TKey, TValue> {
    // wraps GML ds_map
    public number Count { get; }
    public TValue this[TKey key] { get; set; }
    public void Add(TKey key, TValue value) { ... }
    public void Remove(TKey key) { ... }
    public bool ContainsKey(TKey key) { ... }
    public TKey[] Keys { get; }
    public TValue[] Values { get; }
    public void Destroy() { ... }   // must be called to free ds_map
}
```

### 20.3 Math

```tgml
public class Math {
    public const number PI = 3.14159265358979;
    public static number Abs(number x) { ... }
    public static number Floor(number x) { ... }
    public static number Ceil(number x) { ... }
    public static number Round(number x) { ... }
    public static number Sqrt(number x) { ... }
    public static number Pow(number base, number exp) { ... }
    public static number Sin(number x) { ... }
    public static number Cos(number x) { ... }
    public static number Tan(number x) { ... }
    public static number Min(number a, number b) { ... }
    public static number Max(number a, number b) { ... }
    public static number Clamp(number value, number min, number max) { ... }
    public static number Lerp(number a, number b, number t) { ... }
    public static number Random(number n) { ... }
    public static number RandomRange(number min, number max) { ... }
}
```

> Note: `Math` uses `static` internally for BCL implementation only. User code cannot declare `static`.

### 20.4 String Utilities

```tgml
public class StringUtils {
    public static number Length(string s) { ... }
    public static string ToUpper(string s) { ... }
    public static string ToLower(string s) { ... }
    public static string Substring(string s, number start, number length) { ... }
    public static bool Contains(string s, string sub) { ... }
    public static string Replace(string s, string old, string newStr) { ... }
    public static number IndexOf(string s, string sub) { ... }
    public static string[] Split(string s, string delimiter) { ... }
    public static string Trim(string s) { ... }
    public static string Concat(string a, string b) { ... }
    public static string FromNumber(number n) { ... }
    public static number ToNumber(string s) { ... }
}
```

### 20.5 Built-in Delegates

```tgml
public delegate void Action();
public delegate void Action<T>(T arg);
public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
// ... Action<T1..T8>

public delegate TResult Func<TResult>();
public delegate TResult Func<T, TResult>(T arg);
public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
// ... Func<T1..T8, TResult>
```

### 20.6 BCL Runtime Helpers (emitted as GML scripts)

```gml
function __tgml_invoke_delegate(delegate, ...) { ... }
function __tgml_delegate_remove(delegate, fn) { ... }
function __tgml_default(typeName) { ... }
```

---

## 21. Compiler Architecture

### 21.1 Overview

```
Source files (.tgml)
        │
        ▼
   [Lexer / Parser]          ANTLR4-generated C# lexer + parser
        │
        ▼
   [AST Construction]        Visitor pattern over ANTLR parse tree → typed AST nodes
        │
        ▼
   [Phase 1: Population]     Build symbol table (types, members, namespaces, scopes)
        │
        ▼
   [Phase 2: Verification]   Type checking, access control, abstract completeness,
                              generic constraint satisfaction, readonly/const checks,
                              delegate signature compatibility, @Object rules
        │
        ▼
   [Phase 3: Emission]       Per-type emitters produce GML source files
        │
        ▼
   Output .gml files + #macro files + BCL runtime helpers
```

### 21.2 Phase 1 — Population

- Walk all files, register all type declarations (classes, structs, interfaces, enums, delegates) into a global `SymbolTable`.
- Register all members (fields, properties, methods, constructors, indexers) into their owning type's `TypeSymbol`.
- Resolve `using` directives and namespace imports.
- Do **not** resolve method bodies yet.
- Detect duplicate type names within a namespace (error).
- Build inheritance chains; detect circular inheritance (error).

### 21.3 Phase 2 — Verification

Walk each type and verify:

- All `abstract` members are implemented in concrete subclasses.
- `sealed` classes are not subclassed.
- All interface members are implemented.
- All `override` members have a matching `virtual`/`abstract` ancestor.
- Access modifiers are respected at every call site.
- `readonly` fields are not assigned outside constructors.
- `const` fields are assigned compile-time constant expressions.
- `with` is only used on `@Object` instances.
- Lambdas do not capture outer-scope variables.
- Type inference for `var` is unambiguous.
- Named arguments match parameter names.
- Default parameter values are constant expressions.
- Generic constraints are satisfied at each instantiation site.
- `throw` expressions are of type `Exception`.
- All `switch` case labels are compile-time constants.
- No implicit `number` ↔ `bool` conversion occurs.
- Emit errors and warnings with file/line/column.

### 21.4 Phase 3 — Emission

One **Emitter** class per construct:
- `ClassEmitter`
- `StructEmitter`
- `InterfaceEmitter` (emits nothing; used for verification only — interfaces have no GML output)
- `EnumEmitter`
- `DelegateEmitter`
- `MethodEmitter`
- `PropertyEmitter`
- `ConstructorEmitter`
- `ExpressionEmitter`
- `StatementEmitter`
- `DecoratorProcessor` (runs before emitters, transforms the AST/symbol table based on decorators)

Each emitter takes a verified AST node + symbol table context and produces a string of GML.

**Output writer** collects GML strings and writes to the appropriate files (per naming convention in §19.1).

### 21.5 Error Reporting

Errors include:
- Error code (e.g. `TGML0001`)
- Severity: `error` / `warning`
- File path, line number, column number
- Human-readable message

Compilation halts after the Population phase if any structural errors are found. Verification errors are all collected before halting.

---

## 22. Error Catalogue

| Code | Phase | Description |
|---|---|---|
| TGML0001 | Population | Duplicate type name in namespace |
| TGML0002 | Population | Circular inheritance |
| TGML0003 | Verification | Abstract member not implemented in concrete class |
| TGML0004 | Verification | Instantiation of abstract class |
| TGML0005 | Verification | Subclassing a sealed class |
| TGML0006 | Verification | Interface member not implemented |
| TGML0007 | Verification | `override` has no matching `virtual`/`abstract` ancestor |
| TGML0008 | Verification | Access violation (private/protected member accessed from invalid scope) |
| TGML0009 | Verification | `readonly` field assigned outside constructor |
| TGML0010 | Verification | `const` field assigned non-constant expression |
| TGML0011 | Verification | `with` used on non-`@Object` instance |
| TGML0012 | Verification | Lambda captures outer-scope variable (closures not supported) |
| TGML0013 | Verification | Type inference for `var` is ambiguous (e.g. RHS is `null`) |
| TGML0014 | Verification | Named argument does not match any parameter name |
| TGML0015 | Verification | Default parameter value is not a compile-time constant |
| TGML0016 | Verification | Generic constraint not satisfied |
| TGML0017 | Verification | `throw` expression is not of type `Exception` |
| TGML0018 | Verification | Switch case label is not a compile-time constant |
| TGML0019 | Verification | Implicit conversion between `number` and `bool` |
| TGML0020 | Verification | Duplicate case label in switch |
| TGML0021 | Verification | Missing `return` in non-void method |
| TGML0022 | Verification | Type mismatch in assignment or argument |
| TGML0023 | Verification | `@Object` decorator missing object name string |
| TGML0024 | Verification | Multiple `@Object` decorators on one class |
| TGML0025 | Verification | `new` on `@Object` class with wrong parameter count (not x, y, layer + extras) |
| TGML0026 | Verification | `static` modifier used (not supported) |
| TGML0027 | Verification | `global` modifier used on field (only properties) |
| TGML0028 | Verification | Delegate signature mismatch on `+=` / `-=` |
| TGML0029 | Verification | Event assigned directly from outside declaring class |
| TGML0030 | Verification | `null` assigned to non-nullable type |
| TGML0031 | Population | Namespace conflict with existing type name |
| TGML0032 | Verification | Struct inherits from another type (not allowed) |
| TGML0033 | Verification | `break` or `continue` outside loop or switch |
| TGML0034 | Verification | `return` value in void method or missing in non-void method |

---

## Appendix A — Design Decisions Summary

| Decision | Choice | Rationale |
|---|---|---|
| Logical operators | `and`, `or`, `not` | GML style; avoid C-style operator confusion. |
| No closures | Lambdas only; no outer capture | GML has no native closure support; struct-wrapping adds complexity. |
| All reference types | Classes and structs both reference | GML structs are always by reference; value semantics would require deep-copy helpers. |
| No vtable | Inline parent bodies in overrides | Avoids runtime dispatch overhead; GML structs don't support function pointer tables natively. |
| No `__types` | Type info compile-time only | Reduces runtime overhead; `is`/`as` are LSP tools, not runtime checks. |
| `const` → `#macro` | GML macro system | Native GML constant mechanism; zero runtime cost. |
| Delegate = array | Multicast via array | Simple GML-native mechanism; no closure magic needed. |
| Generics = type erasure | `__genericArgs` for runtime names only | One GML function per generic method; no monomorphization bloat. |
| No `foreach` | Omitted | Requires IEnumerable state machine; not worth the complexity at this stage. |
| No string interpolation | Omitted | Use `+` concatenation. |
| No `static` | Only `global` properties | GML global variables cover the use case; avoids class-level state complexity. |
