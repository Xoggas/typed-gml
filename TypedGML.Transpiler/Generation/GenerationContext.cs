using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Decorators;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class GenerationContext
{
    private readonly Stack<Dictionary<string, string>> _identifierAliasScopes = new();
    private readonly Stack<HashSet<string>> _localShadowScopes = new();
    private int _tempCounter;

    public required TranspileContext TranspileContext { get; init; }
    public required DecoratorRegistry DecoratorRegistry { get; init; }

    public TgmlTypeDecl? CurrentType { get; set; }
    public string? WithAlias { get; set; }
    public string SelfAlias { get; set; } = "self";
    public bool InsideGetter { get; set; }
    public bool InsideSetter { get; set; }
    public string? CurrentPropertyName { get; set; }
    public string? CurrentMethodName { get; set; }
    public bool CurrentMethodIsOverride { get; set; }
    public TgmlClassDecl? CurrentMethodOwnerType { get; set; }
    public string? CurrentNativeEventName { get; set; }

    public TypeTable TypeTable => TranspileContext.TypeTable;

    public bool IsGameObject(TgmlTypeDecl decl) => decl is TgmlClassDecl cls && cls.IsGameObject;

    public bool IsGameObjectByName(string typeName)
        => TypeTable.TryResolve(typeName, out var t) && t is TgmlClassDecl c && c.IsGameObject;

    public string GmlObjectName(TgmlClassDecl cls) => cls.GmlObjectName ?? cls.Name;
}
