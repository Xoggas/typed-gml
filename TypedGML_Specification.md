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

### 3.2 BCL Dictionary

`Dictionary<TKey, TValue>` uses **Python-style brace literal** syntax for initialization:

```tgml
Dictionary<string, number> scores = {"Alice": 10, "Bob": 20};
Dictionary<number, string> labels = {0: "zero", 1: "one"};
Dictionary<string, number> empty = {};
```

- Keys and values are **expressions**, not identifiers — any valid expression of the correct type works as a key.
- Key type and value type are inferred from the variable's declared `Dictionary<TKey, TValue>` type.
- Assigning a brace literal to anything other than `Dictionary<K, V>` is a compile error (brace literals are not anonymous structs).
- Duplicate keys in a literal are a compile error (`TGML0042: Duplicate key in Dictionary literal`).

**GML emission:**

```tgml
Dictionary<string, number> scores = {"Alice": 10, "Bob": 20};
```
```gml
// Non-empty literal — emitted as IIFE using BCL constructor and Add():
(function() {
    var __d = Dictionary_create();
    Dictionary_Add(__d, "Alice", 10);
    Dictionary_Add(__d, "Bob", 20);
    return __d;
})()

// Empty literal {}:
Dictionary_create()   // no IIFE needed
```

The compiler emits calls to BCL-generated GML functions (`Dictionary_create`, `Dictionary_Add`) derived via `NamingConvention`. It contains zero `ds_map_*` strings — all GML data structure details live in `bcl/Collections/Dictionary.tgml`.

`ds_map_destroy` must be called manually via `scores.Destroy()` (mapped to `@NativeCall("ds_map_destroy")`).
### 3.3 BCL List

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

### 8.3 Constructors

```tgml
public constructor(number x, number y) : base(x) {
    _x = x;
    _y = y;
}

public constructor(number x) : this(x, 0) { }  // constructor chaining
```

- `base(...)` calls the parent constructor.
- `this(...)` delegates to another constructor in the same class.
- `base(...)` / `this(...)` executes before the constructor body.

### 8.4 Static Constructor

A class or struct may declare one **static constructor** to initialize static members:

```tgml
public class Config {
    public static number Volume;
    public static string Language;

    static constructor() {
        Volume = 1;
        Language = "en";
    }
}
```

Rules:
- No access modifier, no parameters.
- At most one per class or struct — duplicate is a compile error (`TGML0043`).
- Cannot reference `this` or instance members.
- A static constructor is **generated implicitly** if any static field has an initializer expression (even if the developer did not write one explicitly).
- Cross-class static dependencies are **forbidden**: a static constructor body may not reference static members of another class (`TGML0044`). This avoids undefined initialization order.

**GML emission:**

```gml
// Generated function:
function Config_static_ctor() {
    global.Config_Volume = 1;
    global.Config_Language = "en";
}

// Registered via pragma (emitted in the same output file):
gml_pragma("global", "Config_static_ctor()");
```

The `gml_pragma` call causes GML to run the function before any game code executes.

### 8.5 Properties

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
- `static` modifier on a property — see §16.2 and §19.6 for emission rules.

Abstract property:
```tgml
public abstract number Value { get; set; }
```

### 8.6 Methods

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

### 8.7 Indexers

```tgml
public number this[number index] {
    get { return _items[index]; }
    set { _items[index] = value; }
}
```

Multiple indexers with different index types are allowed.
Read-only indexers: omit `set`.

### 8.8 new — Object Instantiation

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

### 8.9 Abstract Classes

- Cannot be instantiated. `new AbstractClass(...)` is a **compile error**.
- All `abstract` members must be overridden in concrete subclasses. Failure to do so is a **compile error**.

### 8.10 Sealed Classes

- Cannot be subclassed. Inheriting from a `sealed` class is a **compile error**.

### 8.11 object Base Type

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

    public constructor(number x, number y) {
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
    public constructor(T value) { _value = value; }
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
- `MyGame.Entities.Player` → GML identifier `MyGame_Entities_Player`

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
| `static` | method, property, field | All static members emit as `global.*` variables in GML. Static methods become `global.ClassName_Method = function(...) { }` inside the static ctor. Static fields become `global.ClassName_Field`. Static properties emit getter/setter lambdas as `global.ClassName_get_Prop` / `global.ClassName_set_Prop`. Any class or struct with static members gets a `gml_pragma`-registered initializer. |
| `abstract` | class, method, property | Class cannot be instantiated; members must be overridden. |
| `sealed` | class | Cannot be subclassed. |
| `virtual` | method, property | Can be overridden in subclasses. |
| `override` | method, property | Overrides a `virtual` or `abstract` member. |
| `readonly` | field | Can only be assigned in the constructor. Compile-time check. |
| `const` | field | Compile-time constant. Emits as GML `#macro`. |

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
| `@Object`-decorated class | One `.gml` file **per GML event** (Create, Step, Draw, etc.) for each overridden event method. Named per GmlEventMap. |
| Top-level functions | One `.gml` script file per function, named after the function. |
| Class / struct with static members | Static ctor function + `gml_pragma("global", "ClassName_static_ctor()")` appended to the class's output file. |
| `const` fields | `#macro ClassName_FieldName value` emitted at the top of the class's output file. |
| `enum` members | `#macro EnumName_MemberName value` emitted into a dedicated enums file per namespace. |

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

### 19.6 Static Member Emission

All `static` members emit as `global.*` entries. No top-level GML script functions are generated for static members.

**Static method:**
```tgml
public class Math {
    public static number Abs(number x) { ... }
}
```
```gml
// in Math.gml — static ctor wraps all static members:
function Math_static_ctor() {
    global.Math_Abs = function(x) {
        return abs(x);
    };
}
gml_pragma("global", "Math_static_ctor()");
```

Call site: `Math.Abs(-5)` → `global.Math_Abs(-5)`

**Static field:**
```tgml
public class Config {
    public static number Volume = 1;
}
```
```gml
function Config_static_ctor() {
    global.Config_Volume = 1;
}
gml_pragma("global", "Config_static_ctor()");
```

**Static property:**
```tgml
public class Config {
    public static number Volume { get; set; }
}
```
```gml
function Config_static_ctor() {
    global.Config_get_Volume = function() { return global.Config_Volume; };
    global.Config_set_Volume = function(value) { global.Config_Volume = value; };
}
gml_pragma("global", "Config_static_ctor()");
```

**Naming convention for static members:**

| TypedGML | GML |
|---|---|
| `ClassName.StaticMethod(args)` | `global.ClassName_StaticMethod(args)` |
| `ClassName.StaticField` | `global.ClassName_StaticField` |
| `ClassName.StaticProp` (get) | `global.ClassName_get_StaticProp()` |
| `ClassName.StaticProp` (set) | `global.ClassName_set_StaticProp(value)` |
| static ctor function | `ClassName_static_ctor` |

### 19.7 @Object Constructor Emission

All `@Object` classes must explicitly extend `TypedGML.GameObjects.GameObject`. The `@Object` decorator binds the class to a GML object asset name used in `instance_create_layer`.

```tgml
@Object("OBJ_Player")
public class Player : GameObject {
    public number Health;
    public constructor(number x, number y, string layer, number health) { ... }
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

Event methods overridden from `GameObject` are emitted into separate GML event files. Only overridden events generate output files — unoverridden events produce no GML file.

### 19.8 Static Constructor Emission

If a class has **any** static members (methods, fields, properties) or an explicit `static constructor()` body, the compiler generates a single `ClassName_static_ctor` GML function containing:

1. All `global.ClassName_Method = function(...) { }` assignments for static methods.
2. All `global.ClassName_Field = initializer` assignments for static fields (explicit initializer or `default(T)`).
3. All `global.ClassName_get_Prop` / `global.ClassName_set_Prop` assignments for static properties.
4. The body of the explicit static constructor (if declared), appended after the above.

The function is registered with:
```gml
gml_pragma("global", "ClassName_static_ctor()");
```

This line is emitted immediately after the function definition in the same output file.

**Output file:** static constructor goes into the class's normal output file (same as instance methods). No separate file is generated.

**Order within the static ctor:** methods first, then fields, then properties, then explicit body. This ensures methods exist before the explicit body can call them.

### 19.9 Delegate Emission

```tgml
myDelegate += Handler;
// emits:
myDelegate[array_length(myDelegate)] = Handler;

myDelegate -= Handler;
// emits:
myDelegate = __tgml_delegate_remove(myDelegate, Handler);

myDelegate(arg1, arg2);
// emits:
__tgml_invoke_delegate(myDelegate, arg1, arg2);
```

### 19.10 Nullable Emission

```tgml
obj?.Field
// emits:
(obj != undefined ? obj.Field : undefined)

a ?? b
// emits:
(a != undefined ? a : b)
```

### 19.11 Primitive Type BCL Mapping

The compiler recognises the following aliases and maps them to BCL types:

| TypedGML keyword | BCL file | BCL type name |
|---|---|---|
| `number` | `bcl/Number.tgml` | `Number` |
| `string` | `bcl/String.tgml` | `String` |
| `bool` | `bcl/Bool.tgml` | `Bool` |

`object.ToString()` is defined in `bcl/Object.tgml` and inherited by all classes and structs. `GetType()` is compiler-hardcoded: it emits a string literal at the call site regardless of BCL.

### 19.12 Operator Intrinsic Shims (`__op_*`)

BCL operator declarations use `@NativeCall("__op_*")` names. These are **compiler-intrinsic** — the emitter recognises them and instead of generating a function call, emits the corresponding GML infix operator directly:

| `@NativeCall` name | Emitted GML |
|---|---|
| `__op_add` | `a + b` |
| `__op_sub` | `a - b` |
| `__op_mul` | `a * b` |
| `__op_div` | `a / b` |
| `__op_mod` | `a % b` |
| `__op_neg` | `-a` |
| `__op_bitnot` | `~a` |
| `__op_eq` | `a == b` |
| `__op_neq` | `a != b` |
| `__op_lt` | `a < b` |
| `__op_gt` | `a > b` |
| `__op_lte` | `a <= b` |
| `__op_gte` | `a >= b` |
| `__op_str_num_add` | `a + string(b)` |
| `__op_num_str_add` | `string(a) + b` |

Any `@NativeCall` name starting with `__op_` is treated as an intrinsic. Non-intrinsic `@NativeCall` names emit as regular GML function calls.

---

## 20. BCL (Base Class Library)

The BCL is written in TypedGML. All BCL files are compiled before user files.

`@NativeCall("gmlName")` on a method means the body is ignored; the emitter substitutes a direct GML call to the named function.

### 20.1 Object — `bcl/Object.tgml`

```tgml
public class Object {
    public virtual string ToString() {
        return "<object>";
    }
}
```

All classes and structs implicitly extend `Object`. `GetType()` is NOT defined here — the compiler emits a string literal at every `GetType()` call site.

### 20.2 Exception — `bcl/Exception.tgml`

```tgml
public struct Exception {
    private string message;
    private string longMessage;
    private string script;
    private string[] stacktrace;

    public constructor(string message) {
        this.message = message;
        this.longMessage = message;
        this.script = "";
        this.stacktrace = [];
    }

    public string Message {
        get { return message; }
    }

    public string LongMessage {
        get { return longMessage; }
    }

    public string Script {
        get { return script; }
    }

    public string[] Stacktrace {
        get { return stacktrace; }
    }

    public override string ToString() {
        return message;
    }
}
```

`Exception` is a navigation class for GML native exception structs, whose fields are `message`, `longMessage`, `script`, and `stacktrace`. Only `Exception` is throwable/catchable.

`throw new Exception("msg")` emits native GML string throwing:

```gml
throw "msg";
```

The compiler does not emit a struct literal or call `Exception_create()` for throws. When a runtime exception is caught, the catch variable is typed as `Exception`; `e.Message`, `e.LongMessage`, `e.Script`, and `e.Stacktrace` read the native fields (`e.message`, `e.longMessage`, `e.script`, `e.stacktrace`). Direct lowercase native field reads are also valid in catch/navigation contexts.

### 20.3 Number — `bcl/Number.tgml`

Defines the `Number` type. The compiler treats `number` as an alias for `Number`.

All valid operations on `number` values are declared here as `static operator` members. `OperatorCheck` resolves primitive operators by looking these up in `SymbolTable` — no special cases in the compiler.

```tgml
public class Number {
    // Arithmetic operators
    public static number operator +(number a, number b) { ... }  // @NativeCall("__op_add")
    public static number operator -(number a, number b) { ... }  // @NativeCall("__op_sub")
    public static number operator *(number a, number b) { ... }  // @NativeCall("__op_mul")
    public static number operator /(number a, number b) { ... }  // @NativeCall("__op_div")
    public static number operator %(number a, number b) { ... }  // @NativeCall("__op_mod")

    // Unary operators
    public static number operator -(number a) { ... }            // @NativeCall("__op_neg")
    public static number operator ~(number a) { ... }            // @NativeCall("__op_bitnot")

    // Comparison operators
    public static bool operator ==(number a, number b) { ... }   // @NativeCall("__op_eq")
    public static bool operator !=(number a, number b) { ... }   // @NativeCall("__op_neq")
    public static bool operator <(number a, number b) { ... }    // @NativeCall("__op_lt")
    public static bool operator >(number a, number b) { ... }    // @NativeCall("__op_gt")
    public static bool operator <=(number a, number b) { ... }   // @NativeCall("__op_lte")
    public static bool operator >=(number a, number b) { ... }   // @NativeCall("__op_gte")

    // String concatenation with number (string + number)
    public static string operator +(string a, number b) { ... }  // @NativeCall("__op_str_num_add")
    public static string operator +(number a, string b) { ... }  // @NativeCall("__op_num_str_add")

    // Utility methods
    public static string ToString(number value) { ... }           // @NativeCall("string")
    public static bool IsNaN(number value) { ... }                // @NativeCall("is_nan")
    public static bool IsInfinity(number value) { ... }          // @NativeCall("is_infinity")
    public static number Parse(string s) { ... }                  // @NativeCall("real")
    public static number Floor(number x) { ... }                  // @NativeCall("floor")
    public static number Ceil(number x) { ... }                   // @NativeCall("ceil")
    public static number Round(number x) { ... }                  // @NativeCall("round")
}
```

> `@NativeCall("__op_add")` etc. are compiler-intrinsic shims that emit the raw GML operator inline (e.g. `a + b`). The `__op_*` names are reserved and handled by the emitter as special cases that produce infix GML rather than function calls.

### 20.4 String — `bcl/String.tgml`

Defines the `String` type. The compiler treats `string` as an alias for `String`.

String indices are **1-based** (GML-native). This is intentional — TypedGML targets GML and does not hide GML conventions.

```tgml
public class String {
    // Operators
    public static string operator +(string a, string b) { ... }  // @NativeCall("__op_add")
    public static bool operator ==(string a, string b) { ... }   // @NativeCall("__op_eq")
    public static bool operator !=(string a, string b) { ... }   // @NativeCall("__op_neq")

    // Methods
    public static number Length(string s) { ... }                          // @NativeCall("string_length")
    public static string ToUpper(string s) { ... }                         // @NativeCall("string_upper")
    public static string ToLower(string s) { ... }                         // @NativeCall("string_lower")
    public static string Substring(string s, number pos, number len) { ... } // @NativeCall("string_copy") — 1-based
    public static string Repeat(string s, number count) { ... }            // @NativeCall("string_repeat")
    public static string Reverse(string s) { ... }                         // @NativeCall("string_reverse")
    public static number IndexOf(string s, string sub) { ... }             // @NativeCall("string_pos") — 1-based; 0 = not found
    public static number IndexOfFrom(string s, string sub, number pos) { ... } // @NativeCall("string_pos_ext")
    public static string Replace(string s, string old, string newStr) { ... }  // @NativeCall("string_replace")
    public static string ReplaceAll(string s, string old, string newStr) { ... } // @NativeCall("string_replace_all")
    public static bool Contains(string s, string sub) { ... }              // string_pos(sub, s) > 0
    public static number Count(string s, string sub) { ... }               // @NativeCall("string_count")
    public static string Trim(string s) { ... }                            // BCL helper
    public static string TrimStart(string s) { ... }                       // BCL helper
    public static string TrimEnd(string s) { ... }                         // BCL helper
    public static string[] Split(string s, string delimiter) { ... }       // BCL loop using string_pos + string_copy
    public static string FromNumber(number n) { ... }                      // @NativeCall("string")
    public static number ToNumber(string s) { ... }                        // @NativeCall("real")
    public static string CharAt(string s, number pos) { ... }              // @NativeCall("string_char_at") — 1-based
    public static number OrdAt(string s, number pos) { ... }               // @NativeCall("string_ord_at")
    public static string FromChar(number ord) { ... }                      // @NativeCall("chr")
}
```

### 20.5 Bool — `bcl/Bool.tgml`

Defines the `Bool` type. The compiler treats `bool` as an alias for `Bool`.

`and`, `or`, `not` are **not** defined as operators here — they are compiler-intrinsic logical keywords that always require `bool` operands and cannot be overloaded.

```tgml
public class Bool {
    // Equality operators
    public static bool operator ==(bool a, bool b) { ... }   // @NativeCall("__op_eq")
    public static bool operator !=(bool a, bool b) { ... }   // @NativeCall("__op_neq")

    // Utility methods
    public static string ToString(bool value) { ... }    // value ? "true" : "false"
    public static bool Parse(string s) { ... }           // s == "true"
}
```

### 20.6 Array utilities — `bcl/ArrayUtils.tgml`

Static utility class for raw `T[]` arrays. Array indices are 0-based (GML arrays are 0-based).

```tgml
public class ArrayUtils {
    public static number Length(object[] arr) { ... }              // @NativeCall("array_length")
    public static object[] Copy(object[] arr) { ... }              // @NativeCall("array_copy") wrapper
    public static object[] Slice(object[] arr, number pos, number len) { ... }  // BCL loop
    public static number IndexOf(object[] arr, object value) { ... }  // @NativeCall("array_find_index")
    public static bool Contains(object[] arr, object value) { ... }   // @NativeCall("array_contains")
    public static object[] Concat(object[] a, object[] b) { ... }     // @NativeCall("array_concat")
    public static void Reverse(object[] arr) { ... }                  // @NativeCall("array_reverse")
    public static void Sort(object[] arr, bool ascending) { ... }     // @NativeCall("array_sort")
    public static object[] Filter(object[] arr, Func<object, bool> predicate) { ... }  // BCL loop
    public static object[] Map(object[] arr, Func<object, object> fn) { ... }          // BCL loop
    public static void ForEach(object[] arr, Action<object> fn) { ... }               // BCL loop
}
```

### 20.7 List — `bcl/Collections.tgml`

```tgml
public class List<T> {
    private T[] _data;

    public constructor() {
        _data = [];
    }

    public number Count {
        get { return Array.Length(_data); }
    }

    public T this[number index] {
        get { return _data[index]; }
        set { _data[index] = value; }
    }

    public void Add(T item) { ... }        // @NativeCall("array_push")  on _data
    public void RemoveAt(number index) { ... } // @NativeCall("array_delete")
    public void Clear() { ... }            // _data = []
    public bool Contains(T item) { ... }   // @NativeCall("array_contains")
    public number IndexOf(T item) { ... }  // @NativeCall("array_find_index")
    public void Insert(number index, T item) { ... }  // @NativeCall("array_insert")
    public void Reverse() { ... }          // @NativeCall("array_reverse")
    public void Sort(bool ascending) { ... }  // @NativeCall("array_sort")
    public T[] ToArray() { ... }           // @NativeCall("array_copy") of _data

    public void Remove(T item) {
        var idx = IndexOf(item);
        if (idx >= 0) { RemoveAt(idx); }
    }

    public override string ToString() {
        return "List[" + Count + "]";
    }
}
```

### 20.8 Dictionary — `bcl/Collections.tgml` (continued)

```tgml
public struct KeyValuePair<TKey, TValue> {
    public TKey Key;
    public TValue Value;

    public constructor(TKey key, TValue value) {
        Key = key;
        Value = value;
    }
}

public class Dictionary<TKey, TValue> {
    private number _map;   // ds_map handle

    public constructor() {
        _map = __ds_map_create();    // @NativeCall("ds_map_create")
    }

    public number Count {
        get { return __ds_map_size(_map); }  // @NativeCall("ds_map_size")
    }

    public TValue this[TKey key] {
        get { return __ds_map_find_value(_map, key); }   // @NativeCall("ds_map_find_value")
        set { __ds_map_set(_map, key, value); }          // @NativeCall("ds_map_set")
    }

    public void Add(TKey key, TValue value) { ... }       // @NativeCall("ds_map_add")
    public void Remove(TKey key) { ... }                  // @NativeCall("ds_map_delete")
    public bool ContainsKey(TKey key) { ... }             // @NativeCall("ds_map_exists")
    public void Destroy() { ... }                         // @NativeCall("ds_map_destroy")

    public TKey[] Keys() {
        return __ds_map_keys_to_array(_map);    // @NativeCall("ds_map_keys_to_array")
    }

    public TValue[] Values() {
        return __ds_map_values_to_array(_map);  // @NativeCall("ds_map_values_to_array")
    }

    public KeyValuePair<TKey, TValue>[] Entries() {
        var keys = Keys();
        var count = ArrayUtils.Length(keys);
        var result = [];
        for (var i = 0; i < count; i += 1) {
            result[i] = new KeyValuePair<TKey, TValue>(keys[i], this[keys[i]]);
        }
        return result;
    }

    public override string ToString() {
        return "Dictionary[" + Count + "]";
    }
}
```

### 20.9 Math — `bcl/Math.tgml`

```tgml
public class Math {
    public const number PI  = 3.14159265358979;
    public const number TAU = 6.28318530717959;
    public const number E   = 2.71828182845905;

    public static number Abs(number x) { ... }           // @NativeCall("abs")
    public static number Floor(number x) { ... }         // @NativeCall("floor")
    public static number Ceil(number x) { ... }          // @NativeCall("ceil")
    public static number Round(number x) { ... }         // @NativeCall("round")
    public static number Sqrt(number x) { ... }          // @NativeCall("sqrt")
    public static number Sqr(number x) { ... }           // @NativeCall("sqr")
    public static number Power(number base, number exp) { ... }  // @NativeCall("power")
    public static number Exp(number x) { ... }           // @NativeCall("exp")
    public static number Log(number x) { ... }           // @NativeCall("log2") — natural log via ln
    public static number Log2(number x) { ... }          // @NativeCall("log2")
    public static number Log10(number x) { ... }         // @NativeCall("log10")
    public static number Ln(number x) { ... }            // @NativeCall("ln")
    public static number Sin(number x) { ... }           // @NativeCall("sin")
    public static number Cos(number x) { ... }           // @NativeCall("cos")
    public static number Tan(number x) { ... }           // @NativeCall("tan")
    public static number ArcSin(number x) { ... }        // @NativeCall("arcsin")
    public static number ArcCos(number x) { ... }        // @NativeCall("arccos")
    public static number ArcTan(number x) { ... }        // @NativeCall("arctan")
    public static number ArcTan2(number y, number x) { ... } // @NativeCall("arctan2")
    public static number DegToRad(number deg) { ... }    // @NativeCall("degtorad")
    public static number RadToDeg(number rad) { ... }    // @NativeCall("radtodeg")
    public static number Min(number a, number b) { ... } // @NativeCall("min")
    public static number Max(number a, number b) { ... } // @NativeCall("max")
    public static number Clamp(number value, number min, number max) { ... } // @NativeCall("clamp")
    public static number Lerp(number a, number b, number t) { ... }         // @NativeCall("lerp")
    public static number Frac(number x) { ... }          // @NativeCall("frac")
    public static number Sign(number x) { ... }          // @NativeCall("sign")
    public static number Mean(number a, number b) { ... } // @NativeCall("mean")
    public static number PointDistance(number x1, number y1, number x2, number y2) { ... } // @NativeCall("point_distance")
    public static number PointDirection(number x1, number y1, number x2, number y2) { ... } // @NativeCall("point_direction")
    public static number LengthDir_x(number len, number dir) { ... }  // @NativeCall("lengthdir_x")
    public static number LengthDir_y(number len, number dir) { ... }  // @NativeCall("lengthdir_y")
}
```

### 20.10 Random — `bcl/Random.tgml`

Stateful random number generator wrapping GML's random functions.

```tgml
public class Random {
    public constructor() { }

    public number Next() { ... }                              // @NativeCall("random_get_seed") — random(1)
    public number NextRange(number min, number max) { ... }   // @NativeCall("random_range")
    public number NextInt(number n) { ... }                   // @NativeCall("irandom")
    public number NextIntRange(number min, number max) { ... } // @NativeCall("irandom_range")
    public void SetSeed(number seed) { ... }                  // @NativeCall("random_set_seed")
    public number GetSeed() { ... }                           // @NativeCall("random_get_seed")
    public bool NextBool() { ... }                            // irandom(1) == 1
}
```

Global stateless helpers also available as static:

```tgml
public class GlobalRandom {
    public static void Randomize() { ... }                   // @NativeCall("randomize")
    public static number Range(number min, number max) { ... } // @NativeCall("random_range")
    public static number IRange(number min, number max) { ... } // @NativeCall("irandom_range")
    public static number Choose(object[] options) { ... }    // BCL loop + irandom(length-1)
}
```

### 20.11 Debug — `bcl/Debug.tgml`

```tgml
public class Debug {
    public static void Log(string message) { ... }           // @NativeCall("show_debug_message")
    public static void LogValue(string label, object value) { ... }  // show_debug_message(label + ": " + string(value))
    public static void Assert(bool condition, string message) { ... } // if (!condition) throw new Exception(message)
    public static void Break() { ... }                       // @NativeCall("breakpoint")
}
```

### 20.12 Delegates — `bcl/Delegates.tgml`

```tgml
public delegate void Action();
public delegate void Action<T>(T arg);
public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
public delegate void Action<T1, T2, T3, T4, T5>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5);
public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6);
public delegate void Action<T1, T2, T3, T4, T5, T6, T7>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7);
public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8);

public delegate TResult Func<TResult>();
public delegate TResult Func<T, TResult>(T arg);
public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 a1, T2 a2, T3 a3, T4 a4);
public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5);
public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6);
public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7);
public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8);

public delegate bool Predicate<T>(T arg);
public delegate TResult Converter<TInput, TResult>(TInput input);
public delegate number Comparison<T>(T a, T b);
```

### 20.13 BCL Runtime Helpers — emitted as GML scripts

These are emitted by the compiler directly (not from `.tgml` files):

```gml
function __tgml_invoke_delegate(delegate) {
    var _len = array_length(delegate);
    var _result = undefined;
    for (var _i = 0; _i < _len; _i++) {
        _result = delegate[_i]();   // args passed via ... in real emission
    }
    return _result;
}

function __tgml_delegate_remove(delegate, fn) {
    var _out = [];
    var _len = array_length(delegate);
    for (var _i = 0; _i < _len; _i++) {
        if (delegate[_i] != fn) array_push(_out, delegate[_i]);
    }
    return _out;
}

function __tgml_default(typeName) {
    switch (typeName) {
        case "number": return 0;
        case "bool":   return false;
        default:       return undefined;
    }
}
```

### 20.1 Exception

```tgml
public struct Exception {
    private string message;
    private string longMessage;
    private string script;
    private string[] stacktrace;

    public constructor(string message) { ... }
    public string Message { get; }
    public string LongMessage { get; }
    public string Script { get; }
    public string[] Stacktrace { get; }
}
```

Only `Exception` is throwable/catchable. It navigates GML native exception structs; `throw new Exception("msg")` emits `throw "msg";`.

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
    public void Destroy() { ... }   // must be called to free ds_map; wraps ds_map_destroy
}
```

Literal initialization syntax: `{"key": value, ...}` — see §3.2 for full rules and GML emission.

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
| TGML0026 | Verification | `static` used on indexer or constructor |
| TGML0027 | Verification | `static` used inside an interface |
| TGML0028 | Verification | Delegate signature mismatch on `+=` / `-=` |
| TGML0029 | Verification | Event assigned directly from outside declaring class |
| TGML0030 | Verification | `null` assigned to non-nullable type |
| TGML0031 | Population | Namespace conflict with existing type name |
| TGML0032 | Verification | Struct inherits from another type (not allowed) |
| TGML0033 | Verification | `break` or `continue` outside loop or switch |
| TGML0034 | Verification | `return` value in void method or missing in non-void method |
| TGML0035 | Verification | `return` value in void method or missing in non-void method (control-flow path) |
| TGML0036 | Verification | Static field initializer references another class's static member (cross-class static dependency) |
| TGML0037 | Verification | Unknown `@NativeEvent` logical name |
| TGML0038 | Verification | No overload of method matches the supplied arguments |
| TGML0039 | Verification | Ambiguous call — multiple overloads match |
| TGML0040 | Verification | Duplicate key in Dictionary literal |
| TGML0041 | Verification | Duplicate static constructor in the same class |
| TGML0042 | Verification | Static constructor body references static member of another class |
| TGML0043 | Verification | Static constructor has parameters |
| TGML0044 | Verification | Static constructor has an access modifier |
| TGML0045 | Verification | Class extends `GameObject` but is missing the `@Object` decorator |
| TGML0046 | Verification | Class has `@Object` decorator but does not explicitly extend `GameObject` |

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
| `static` → `global.*` | All static members emit as `global.ClassName_Member` | GML has no class-level scope; `global.*` is the cleanest uniform mapping. One codegen path, no branching between script functions and global vars. |
| Static constructor via `gml_pragma` | `gml_pragma("global", "ClassName_static_ctor()")` | GameMaker's native mechanism for pre-game initialization. Zero runtime overhead, no controller object needed. |
