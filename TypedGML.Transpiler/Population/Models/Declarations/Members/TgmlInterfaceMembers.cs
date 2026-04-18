namespace TypedGML.Transpiler.Population.Models;

/// <summary>An interface accessor stub: get; or set;</summary>
public sealed class TgmlInterfaceAccessorDecl
{
    public bool IsGet { get; init; }
    public bool IsSet => !IsGet;
}

/// <summary>An interface method declaration (no access modifier).</summary>
public sealed class TgmlInterfaceMethodDecl
{
    public List<TgmlDecorator> Decorators { get; init; } = new();
    public required TgmlTypeRef ReturnType { get; init; }
    public required string Name { get; init; }
    public List<TgmlTypeParam> TypeParams { get; init; } = new();
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>null when declared as a stub (semicolon body).</summary>
    public TgmlBlock? Body { get; init; }
}

/// <summary>An interface property declaration (no access modifier).</summary>
public sealed class TgmlInterfacePropertyDecl
{
    public List<TgmlDecorator> Decorators { get; init; } = new();
    public required TgmlTypeRef Type { get; init; }
    public required string Name { get; init; }
    public List<TgmlInterfaceAccessorDecl> Accessors { get; init; } = new();
    public bool HasGetter => Accessors.Any(a => a.IsGet);
    public bool HasSetter => Accessors.Any(a => a.IsSet);
}