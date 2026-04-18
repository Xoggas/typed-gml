# TypedGML — Copilot Instructions

## Суть проекта

TypedGML — статически типизированное надмножество GameMaker Language (GML), транслируемое в GML. Концептуальный аналог TypeScript для JavaScript, но для экосистемы GameMaker. Реализован на C# с использованием ANTLR4.

Проект состоит из двух частей:
- **Язык TypedGML** — синтаксис, система типов, семантика
- **Транслятор TypedGML → GML** — парсер, семантический анализатор, генератор кода

---

## Технологии

- **Язык реализации:** C#
- **Парсер:** ANTLR4 (grammar file: `TypedGML.g4`)
- **Целевой язык:** GML (GameMaker Language)
- **Паттерн обхода AST:** Visitor (генерируется ANTLR4)

---

## Синтаксис TypedGML

TypedGML синтаксически максимально близок к C#. Ключевые особенности:

```csharp
namespace Game.Systems;
using Game.Objects;

public interface IDamageModifier
{
    int Apply(int damage, DamageType type);
    string Name { get; }
}

public class FlatModifier : IDamageModifier
{
    private int _bonus;

    public constructor(int bonus)
    {
        self._bonus = bonus;
    }

    public int Apply(int damage, DamageType type) => damage + _bonus;

    public string Name
    {
        get => "Flat";
    }
}

@Object("obj_Player")
public class Player : GameObject
{
    private int _health;

    public constructor(real x, real y, string layer)
        : base(x, y, layer)
    {
        _health = 100;
    }

    public override void Step()
    {
        with (Enemy enemy)
        {
            enemy.TakeDamage(10);
        }
    }
}
```

### Объявление переменных и полей

Аннотации типов — C#-стиль: тип идёт перед именем. Ключевое слово `var` для локальных переменных с выводом типа (если поддерживается), иначе явный тип:

```csharp
public int Health = 100;
private real _speed = 5.0;
public const int MaxHealth = 200;
public static int TotalKills = 0;

// локальные переменные
int result = 0;
```

### Параметры и возвращаемые типы — C#-стиль

```csharp
public int Add(int a, int b) { return a + b; }
public bool IsAlive() => _health > 0;
public void Step() { ... }
```

### Свойства — C#-стиль с ключевым словом `field`

Свойства объявляются C#-синтаксисом. Ключевое слово `field` внутри геттера/сеттера обращается к автоматически генерируемому backing field — явное поле объявлять не нужно:

```csharp
// Свойство с backing field через field
public int Health
{
    get => field;
    set => field = Math.Clamp(value, 0, 100);
}

// Read-only свойство
public bool IsAlive
{
    get => _health > 0;
}

// Свойство с телом
public int HealthPercent
{
    get
    {
        return (field * 100) / _maxHealth;
    }
    set
    {
        field = (value * _maxHealth) / 100;
    }
}
```

Транслируется в:
```js
function get_Health() { return __backing_Health; }
function set_Health(value) { __backing_Health = Math_Clamp(value, 0, 100); }
```

### Generic constraints — через `:`

Единственное место, где `:` используется для типов — ограничения generic-параметров:

```csharp
public class ModifierList<T : IDamageModifier>
{
    private T[] _items = [];

    public void Add(T item) { ... }
    public T Get(int index) => _items[index];
}
```

### Global-свойства

Глобальные переменные объявляются как свойства с модификатором `global`. Опциональный `virtual` разрешает переопределение в потомках.

Пустые геттер/сеттер транслируются в прямой доступ к `global.PropertyName`. Ключевое слово `field` внутри тела геттера/сеттера тоже ссылается на `global.PropertyName` — это позволяет добавить логику поверх глобальной переменной:

```csharp
// Простой случай — пустое тело
public global int Score
{
    get { }
    set { }
}

// С логикой через field — field = global.MasterVolume
public global virtual real MasterVolume
{
    get { return field; }
    set { field = Math.Clamp(value, 0, 1); }
}

// Переопределение в потомке
public override real MasterVolume
{
    get { return field * 0.5; }
    set { field = value; }
}
```

Транслируется в:
```js
// Score — прямой доступ
// чтение Score → global.Score
// запись Score → global.Score = value

// MasterVolume — field = global.MasterVolume
function get_MasterVolume() { return global.MasterVolume; }
function set_MasterVolume(value) { global.MasterVolume = Math_Clamp(value, 0, 1); }
```

**Типы:** `bool`, `int`, `long`, `real`, `string`, `ptr`, `void`  
**Ассет-типы:** `Sound`, `Sprite`, `Room`, `Font` — несовместимы между собой  
**Массивы:** `int[]`, `T[]`  
**Декораторы:** `@Object("name")`, `@NativeProperty("name")`, `@NativeEvent("name")`  
**Нативные директивы:** строки с `#` — только в методах с модификатором `nocheck`

---

## Трансляция в GML

### Скриптовые классы → GML-структуры

```csharp
public class Vec2 { public real X; public real Y; }
```
```js
function Vec2() constructor { self.X = 0; self.Y = 0; }
```

### Наследование → инлайн-разворачивание

GML не поддерживает наследование структур. Транслятор копирует все поля и методы родителя в потомка. `base.Method()` заменяется инлайн-вставкой тела родительского метода.

### Игровые объекты (@Object)

```csharp
Enemy e = new Enemy(100, 200, "Instances", 50);
```
```js
var e = instance_create_layer(100, 200, "Instances", obj_Enemy);
obj_Enemy_Init(e, 50);
```

Поля конструктора → событие Create. Переопределённые события → отдельные файлы событий GameMaker.

### with

```csharp
with (Enemy enemy) { enemy.TakeDamage(10); }
```
```js
with (obj_Enemy) { TakeDamage(other.damage); }
```

### Пространства имён → префикс

`Game.Objects.Player` → `Game_Objects_Player`

---

## Система типов в рантайме

### `__types` — проверка `is`/`as`

Каждый класс получает поле `__types` — структура-множество с целочисленными ID всех классов и интерфейсов по цепочке наследования:

```js
// #macro __TYPE_Enemy 3
// #macro __TYPE_UnitBase 1
// #macro __TYPE_IDamageModifier 4
__types = { 3: true, 1: true, 4: true }
```

`obj is IDamageModifier` →
```js
is_struct(obj) && variable_struct_exists(obj.__types, __TYPE_IDamageModifier)
```

Транслятор генерирует макросы `#macro __TYPE_ClassName N` для всех типов.

### `__genericArgs` — реификация обобщений

```js
// new ModifierList<FlatModifier>()
var list = new ModifierList(__TYPE_FlatModifier);

function ModifierList(__t0) constructor {
    __types = { 5: true }       // __TYPE_ModifierList
    __genericArgs = [__t0]
}
```

`obj is ModifierList<FlatModifier>` →
```js
is_struct(obj)
&& variable_struct_exists(obj.__types, __TYPE_ModifierList)
&& obj.__genericArgs[0] == __TYPE_FlatModifier
```

Вложенные обобщения хранятся рекурсивно: `__genericArgs = [[__TYPE_ModifierList, [__TYPE_FlatModifier]]]`. Проверка через `__CheckGenericArgs(args, expected)` из стандартной библиотеки.

---

## Структура транслятора

```
TypedGML.g4              — ANTLR4 грамматика
├── Lexer / Parser       — генерируется ANTLR4
├── AST Visitor          — обход дерева, генерируется ANTLR4
├── SemanticAnalyzer     — проверка типов, контрактов, областей видимости
│   ├── TypeTable        — реестр всех типов с их ID
│   ├── SymbolTable      — таблица символов (переменные, методы)
│   └── InheritanceResolver — разворачивание цепочек наследования
└── CodeGenerator        — генерация GML-кода
    ├── ScriptClassEmitter   — скриптовые классы → GML-структуры
    ├── GameObjectEmitter    — @Object классы → события GameMaker
    └── TypeMacroEmitter     — генерация #macro __TYPE_* и __CheckGenericArgs
```

---

## Важные семантические правила

- Модификаторы доступа обязательны везде кроме членов интерфейса
- `abstract` методы — нет тела, `;` вместо блока; потомок обязан переопределить
- `sealed` — запрещает наследование на уровне транслятора, в GML не отражается
- `nocheck` — отключает проверку возвращаемого значения, разрешает `#`-директивы
- `null` транслируется в `undefined`
- `bool` несовместим с `int`, неявное приведение только `int` → `real`
- Статические поля → `global.ClassName_FieldName`
- `self` внутри события игрового объекта транслируется неявно (без префикса)
- `base` внутри события игрового объекта → `event_inherited()`
- `base.Method()` внутри скриптового класса → инлайн тела родителя
- `__types` для игровых объектов инициализируется в событии Create
- `field` в геттере/сеттере обычного свойства → `__backing_PropertyName`
- `field` в геттере/сеттере `global`-свойства → `global.PropertyName`
- `global` свойство с пустым телом → прямой доступ к `global.PropertyName`; `virtual` разрешает переопределение геттера/сеттера в потомках, при этом `field` у всех потомков ссылается на ту же глобальную переменную

---

## Чего НЕ существует в TypedGML

- Nullable типы (`T?`) — отсутствуют
- `foreach` — отсутствует, есть `for` и `with`
- `?.` и `??` операторы — отсутствуют
- `sizeof` — отсутствует (GML не поддерживает)
- Мономорфизация обобщений — только стирание типов + реификация через `__genericArgs`
- `instanceof` — заменён единым механизмом `__types`
- Аннотации типов через `:` — только в generic constraints (`<T : IFoo>`), везде остальное C#-стиль