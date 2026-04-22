using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Decorators;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Contextual state for a single code generation pass over one type declaration.
/// </summary>
public sealed class GenerationContext
{
    public required TranspileContext TranspileContext { get; init; }
    public required DecoratorRegistry DecoratorRegistry { get; init; }

    /// <summary>The type declaration currently being emitted.</summary>
    public TgmlTypeDecl? CurrentType { get; set; }

    /// <summary>If non-null, references to this name inside a with-body are implicit self.</summary>
    public string? WithAlias { get; set; }

    /// <summary>
    ///     The GML name to emit for <c>this</c> / <c>self</c>. Defaults to <c>"self"</c>.
    ///     Set to e.g. <c>"inst"</c> when emitting an Init function body so that
    ///     <c>this.Field = value</c> emits as <c>inst.set_Field(value)</c> rather than
    ///     using a <c>with(inst)</c> block.
    /// </summary>
    public string SelfAlias { get; set; } = "self";

    /// <summary>True while emitting a property getter body.</summary>
    public bool InsideGetter { get; set; }

    /// <summary>True while emitting a property setter body.</summary>
    public bool InsideSetter { get; set; }

    /// <summary>Name of the current property (used for backing field substitution).</summary>
    public string? CurrentPropertyName { get; set; }

    /// <summary>Name of the current method being emitted, when applicable.</summary>
    public string? CurrentMethodName { get; set; }

    /// <summary>True when the current method declaration is marked override.</summary>
    public bool CurrentMethodIsOverride { get; set; }

    /// <summary>
     ///     The declaring type whose method body is currently being emitted.
     ///     This differs from <see cref="CurrentType"/> when inherited GameObject
     ///     members are flattened into a descendant object's Create event.
     /// </summary>
    public TgmlClassDecl? CurrentMethodOwnerType { get; set; }

    private readonly Stack<Dictionary<string, string>> _identifierAliasScopes = new();
    private readonly Stack<HashSet<string>> _localShadowScopes = new();
    private int _tempCounter;

    /// <summary>
    ///     Non-null only while emitting a GameMaker object event body such as Step/Create.
    ///     Used to translate base calls to event_inherited().
    /// </summary>
    public string? CurrentNativeEventName { get; set; }

    public TypeTable TypeTable => TranspileContext.TypeTable;

    public bool IsGameObject(TgmlTypeDecl decl)
    {
        return decl is TgmlClassDecl cls && cls.IsGameObject;
    }

    /// <summary>
    ///     Look up whether a named type is a @Object class.
    /// </summary>
    public bool IsGameObjectByName(string typeName)
    {
        return TypeTable.TryResolve(typeName, out var t) && t is TgmlClassDecl c && c.IsGameObject;
    }

    public string GmlObjectName(TgmlClassDecl cls)
    {
        return cls.GmlObjectName ?? cls.Name;
    }

    /// <summary>
    ///     Returns the property with the given name declared on the current type
    ///     <b>or any ancestor type</b> in the inheritance chain.
    ///     This ensures that inherited properties (e.g. <c>Health</c> on a subclass of <c>Entity</c>)
    ///     are correctly redirected to <c>get_Health()</c> / <c>set_Health()</c> calls.
    /// </summary>
    public TgmlPropertyDecl? FindProperty(string name)
    {
        return PropertyAccessHelper.FindPropertyInHierarchy(TypeTable, CurrentType, name)?.Property;
    }

    public string? GetNativePropertyName(TgmlPropertyDecl? property)
    {
        if (property?.Metadata.TryGetValue("NativePropertyName", out var nativeName) == true &&
            nativeName is string s &&
            !string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        return null;
    }

    public string? GetNativePropertyName(string propertyName)
    {
        return GetNativePropertyName(FindProperty(propertyName));
    }

    public string? GetAssetName(TgmlMemberDecl? member)
    {
        return AssetFacts.TryGetAssetName(member, out var assetName)
            ? assetName
            : null;
    }

    public TgmlPropertyDecl? FindProperty(TgmlExpression target, string propertyName)
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName ||
            !TypeTable.TryResolve(typeName, out var decl) ||
            decl is null)
        {
            return null;
        }

        return PropertyAccessHelper.FindPropertyInHierarchy(TypeTable, decl, propertyName)?.Property;
    }

    public TgmlPropertyDecl? FindIndexer(TgmlExpression target)
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName)
        {
            return null;
        }

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
            return null;

        if (!TypeTable.TryResolve(typeName, out var decl) || decl is null)
            return null;

        return PropertyAccessHelper.FindIndexerInHierarchy(TypeTable, decl)?.Property;
    }

    public bool TryResolveStaticAssetReference(TgmlExpression target, string memberName, out string assetName)
    {
        assetName = string.Empty;
        if (target is not TgmlIdExpr id)
            return false;

        if (!TypeTable.TryResolve(id.Name, out var decl) || decl is null)
            return false;

        return TryResolveStaticAssetReference(decl, memberName, out assetName);
    }

    public bool TryResolveCurrentTypeAssetReference(string memberName, out string assetName)
    {
        assetName = string.Empty;
        return CurrentType is not null &&
               TryResolveStaticAssetReference(CurrentType, memberName, out assetName);
    }

    /// <summary>
    ///     Returns true when we are currently emitting the getter or setter body of the
    ///     named property. Used to prevent infinite recursion when redirecting property
    ///     reads/writes to get_X / set_X calls.
    /// </summary>
    public bool IsInsideAccessorOf(string propertyName)
    {
        return (InsideGetter || InsideSetter) && CurrentPropertyName == propertyName;
    }

    public bool TryFindBaseMethod(
        TgmlClassDecl owner,
        string methodName,
        out TgmlClassDecl declaringType,
        out TgmlMethodDecl method)
    {
        return TryFindBaseMethod(owner.BaseTypes, methodName, new HashSet<string>(StringComparer.Ordinal), out declaringType, out method);
    }

    public List<TgmlClassDecl> GetGameObjectAncestorChain(TgmlClassDecl cls)
    {
        var chain = new List<TgmlClassDecl>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        CollectGameObjectAncestors(cls.BaseTypes, visited, chain);
        return chain;
    }

    public List<TgmlClassDecl> GetClassAncestorChain(TgmlClassDecl cls)
    {
        var chain = new List<TgmlClassDecl>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        CollectClassAncestors(cls.BaseTypes, visited, chain);
        return chain;
    }

    private void CollectGameObjectAncestors(
        IEnumerable<TgmlTypeRef> baseTypes,
        HashSet<string> visited,
        List<TgmlClassDecl> chain)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            CollectGameObjectAncestors(baseClass.BaseTypes, visited, chain);

            if (baseClass.IsGameObject)
                chain.Add(baseClass);
        }
    }

    private void CollectClassAncestors(
        IEnumerable<TgmlTypeRef> baseTypes,
        HashSet<string> visited,
        List<TgmlClassDecl> chain)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            CollectClassAncestors(baseClass.BaseTypes, visited, chain);
            chain.Add(baseClass);
        }
    }

    public void PushIdentifierAliases(IEnumerable<KeyValuePair<string, string>> aliases)
    {
        _identifierAliasScopes.Push(new Dictionary<string, string>(aliases, StringComparer.Ordinal));
    }

    public void PopIdentifierAliases()
    {
        if (_identifierAliasScopes.Count > 0)
            _identifierAliasScopes.Pop();
    }

    public bool TryGetIdentifierAlias(string name, out string alias)
    {
        foreach (var scope in _identifierAliasScopes)
        {
            if (scope.TryGetValue(name, out var foundAlias))
            {
                alias = foundAlias;
                return true;
            }
        }

        alias = string.Empty;
        return false;
    }

    /// <summary>
    ///     Pushes a new local scope with the given names that shadow type-level properties.
    ///     Call <see cref="PopLocalScope"/> when the scope exits.
    /// </summary>
    public void PushLocalScope(IEnumerable<string> names)
        => _localShadowScopes.Push(new HashSet<string>(names, StringComparer.Ordinal));

    public void PopLocalScope()
    {
        if (_localShadowScopes.Count > 0)
            _localShadowScopes.Pop();
    }

    /// <summary>Declares <paramref name="name"/> as a local in the innermost scope.</summary>
    public void DeclareLocal(string name)
    {
        if (_localShadowScopes.Count > 0)
            _localShadowScopes.Peek().Add(name);
    }

    /// <summary>Returns true when <paramref name="name"/> is shadowed by a local variable or parameter.</summary>
    public bool IsLocalShadow(string name)
    {
        foreach (var scope in _localShadowScopes)
            if (scope.Contains(name)) return true;
        return false;
    }

    public string AllocateTempIdentifier(string prefix)
    {
        _tempCounter++;
        return $"__{prefix}_{_tempCounter}";
    }

    private bool TryFindBaseMethod(
        IEnumerable<TgmlTypeRef> baseTypes,
        string methodName,
        HashSet<string> visited,
        out TgmlClassDecl declaringType,
        out TgmlMethodDecl method)
    {
        foreach (var baseRef in baseTypes)
        {
            if (!TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is not TgmlClassDecl baseClass)
                continue;

            var key = baseClass.QualifiedName ?? baseClass.Name;
            if (!visited.Add(key))
                continue;

            var found = baseClass.Methods.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            if (found is not null)
            {
                declaringType = baseClass;
                method = found;
                return true;
            }

            if (TryFindBaseMethod(baseClass.BaseTypes, methodName, visited, out declaringType, out method))
                return true;
        }

        declaringType = null!;
        method = null!;
        return false;
    }

    private bool TryResolveStaticAssetReference(TgmlTypeDecl decl, string memberName, out string assetName)
    {
        assetName = string.Empty;

        switch (decl)
        {
            case TgmlClassDecl cls:
            {
                var property = cls.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(property, out assetName))
                    return true;

                var field = cls.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                return AssetFacts.TryGetAssetName(field, out assetName);
            }

            case TgmlStructDecl str:
            {
                var property = str.Properties.FirstOrDefault(p => p.IsStatic && string.Equals(p.Name, memberName, StringComparison.Ordinal));
                if (AssetFacts.TryGetAssetName(property, out assetName))
                    return true;

                var field = str.Fields.FirstOrDefault(f => f.IsStatic && string.Equals(f.Name, memberName, StringComparison.Ordinal));
                return AssetFacts.TryGetAssetName(field, out assetName);
            }
        }

        return false;
    }
}
