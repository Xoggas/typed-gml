using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Decorators;

/// <summary>
///     Registry of all decorator handlers; applies them to type and member declarations.
/// </summary>
public sealed class DecoratorRegistry
{
    private readonly Dictionary<string, IDecoratorHandler> _handlers = new(StringComparer.Ordinal);

    /// <summary>Builds the default registry with all built-in handlers.</summary>
    public static DecoratorRegistry Default { get; } = new DecoratorRegistry()
        .Register(new ObjectDecoratorHandler())
        .Register(new NativeEventDecoratorHandler())
        .Register(new NativePropertyDecoratorHandler())
        .Register(new AssetDecoratorHandler());

    public DecoratorRegistry Register(IDecoratorHandler handler)
    {
        _handlers[handler.DecoratorName] = handler;
        return this;
    }

    /// <summary>Apply all matching decorators on a type declaration and its members.</summary>
    public void ApplyAll(TgmlTypeDecl type, TranspileContext ctx)
    {
        foreach (var dec in type.Decorators)
        {
            if (_handlers.TryGetValue(dec.SimpleName, out var h))
            {
                h.ApplyToType(dec, type, ctx);
            }
        }

        // Apply to members
        var members = GetMembers(type);
        foreach (var member in members)
        foreach (var dec in member.Decorators)
        {
            if (_handlers.TryGetValue(dec.SimpleName, out var h))
            {
                h.ApplyToMember(dec, member, ctx);
            }
        }
    }

    private static IEnumerable<TgmlMemberDecl> GetMembers(TgmlTypeDecl type)
    {
        if (type is TgmlClassDecl cls)
        {
            foreach (var f in cls.Fields)
            {
                yield return f;
            }

            foreach (var p in cls.Properties)
            {
                yield return p;
            }

            foreach (var m in cls.Methods)
            {
                yield return m;
            }

            foreach (var ctor in cls.Constructors)
            {
                yield return ctor;
            }
        }
        else if (type is TgmlStructDecl st)
        {
            foreach (var f in st.Fields)
            {
                yield return f;
            }

            foreach (var p in st.Properties)
            {
                yield return p;
            }

            foreach (var m in st.Methods)
            {
                yield return m;
            }

            foreach (var ctor in st.Constructors)
            {
                yield return ctor;
            }
        }
    }
}
