namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A single get or set accessor on a property.
///     When <see cref="IsAuto"/> is <c>true</c> the accessor has an empty body and the
///     compiler generates a backing field (<c>__backing_PropertyName</c>) automatically.
/// </summary>
public sealed class TgmlAccessorDecl
{
    /// <summary><c>true</c> for a <c>get</c> accessor; <c>false</c> for <c>set</c>.</summary>
    public bool IsGet { get; init; }

    /// <inheritdoc cref="IsGet"/>
    public bool IsSet => !IsGet;

    /// <summary>
    ///     Overriding access modifier for this accessor (e.g. a <c>private set</c>).
    ///     <c>null</c> means the accessor inherits the property's declared access.
    /// </summary>
    public AccessModifier? AccessMod { get; init; }

    /// <summary>
    ///     The accessor body, or <c>null</c> for auto accessors (<c>get;</c> / <c>set;</c>).
    /// </summary>
    public TgmlBlock? Body { get; init; }

    /// <summary><c>true</c> when this is an auto accessor with no explicit body.</summary>
    public bool IsAuto => Body is null;
}

/// <summary>
///     A property declaration on a class or struct.
///     Properties with auto accessors and no native-property mapping generate a
///     <c>__backing_PropertyName</c> field and corresponding <c>get_</c>/<c>set_</c> GML functions.
/// </summary>
public sealed class TgmlPropertyDecl : TgmlMemberDecl
{
    /// <summary>The declared type of the property.</summary>
    public required TgmlTypeRef Type { get; init; }

    /// <summary>Visibility, static, virtual/override/abstract and global modifiers.</summary>
    public required PropertyModifiers Modifiers { get; init; }

    /// <summary>Index parameter for indexer properties; <c>null</c> for regular properties.</summary>
    public TgmlParam? IndexParam { get; init; }

    /// <summary>Accessor declarations (up to one getter and one setter).</summary>
    public List<TgmlAccessorDecl> Accessors { get; init; } = new();

    /// <summary>The getter accessor, or <c>null</c> if only a setter is declared.</summary>
    public TgmlAccessorDecl? Getter => Accessors.FirstOrDefault(a => a.IsGet);

    /// <summary>The setter accessor, or <c>null</c> if only a getter is declared.</summary>
    public TgmlAccessorDecl? Setter => Accessors.FirstOrDefault(a => a.IsSet);

    /// <summary><c>true</c> when the <c>static</c> modifier is present.</summary>
    public bool IsStatic => Modifiers.Scope == ScopeModifier.Static;

    /// <summary><c>true</c> when this is an indexer property (has an <see cref="IndexParam"/>).</summary>
    public bool IsIndexer => IndexParam is not null;
}
