namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A get or set accessor stub declared inside an interface property
///     (<c>get;</c> or <c>set;</c>).
/// </summary>
public sealed class TgmlInterfaceAccessorDecl
{
    /// <summary><c>true</c> for a <c>get</c> stub; <c>false</c> for a <c>set</c> stub.</summary>
    public bool IsGet { get; init; }

    /// <inheritdoc cref="IsGet"/>
    public bool IsSet => !IsGet;
}

/// <summary>
///     A method declaration inside an interface.
///     Interface methods have no access modifier and may optionally carry a default body.
/// </summary>
public sealed class TgmlInterfaceMethodDecl
{
    /// <summary>Decorators applied to this method (e.g. <c>@NativeCall</c>).</summary>
    public List<TgmlDecorator> Decorators { get; init; } = new();

    /// <summary>The declared return type.</summary>
    public required TgmlTypeRef ReturnType { get; init; }

    /// <summary>The method name.</summary>
    public required string Name { get; init; }

    /// <summary>Generic type parameters declared on this method.</summary>
    public List<TgmlTypeParam> TypeParams { get; init; } = new();

    /// <summary>Ordered list of formal parameters.</summary>
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>
    ///     Optional default body; <c>null</c> when the method is declared as a stub
    ///     (<c>ReturnType Name(…);</c>).
    /// </summary>
    public TgmlBlock? Body { get; init; }
}

/// <summary>
///     A property declaration inside an interface.
///     Interface properties have no access modifier and carry only accessor stubs.
/// </summary>
public sealed class TgmlInterfacePropertyDecl
{
    /// <summary>Decorators applied to this property (e.g. <c>@NativeProperty</c>).</summary>
    public List<TgmlDecorator> Decorators { get; init; } = new();

    /// <summary>The declared type of the property.</summary>
    public required TgmlTypeRef Type { get; init; }

    /// <summary>The property name.</summary>
    public required string Name { get; init; }

    /// <summary>Index parameter for interface indexers; <c>null</c> for regular properties.</summary>
    public TgmlParam? IndexParam { get; init; }

    /// <summary>Accessor stubs declared in the interface body.</summary>
    public List<TgmlInterfaceAccessorDecl> Accessors { get; init; } = new();

    /// <summary><c>true</c> when a <c>get</c> stub is present.</summary>
    public bool HasGetter => Accessors.Any(a => a.IsGet);

    /// <summary><c>true</c> when a <c>set</c> stub is present.</summary>
    public bool HasSetter => Accessors.Any(a => a.IsSet);

    /// <summary><c>true</c> when this property is an indexer.</summary>
    public bool IsIndexer => IndexParam is not null;
}
