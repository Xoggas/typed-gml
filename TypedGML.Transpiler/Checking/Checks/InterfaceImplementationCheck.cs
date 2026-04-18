using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 2: Verifies that non-abstract classes fully implement:
///     <list type="bullet">
///         <item>All methods and properties declared on every interface they list in their base types.</item>
///         <item>All abstract methods inherited from parent classes.</item>
///     </list>
/// </summary>
public sealed class InterfaceImplementationCheck : IAtomicCheck
{
    public string Name => "InterfaceImplementationCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            if (type is TgmlClassDecl cls && !cls.IsAbstract)
                CheckClass(context, file, cls);
    }

    private static void CheckClass(TranspileContext ctx, TgmlFile file, TgmlClassDecl cls)
    {
        var ownMethods = CollectConcreteMethodNames(ctx, cls);
        var ownProps = CollectConcretePropNames(ctx, cls);

        foreach (var baseRef in cls.BaseTypes)
        {
            if (!ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) || baseDecl is null) continue;

            switch (baseDecl)
            {
                case TgmlInterfaceDecl iface:
                    CheckInterface(ctx, file, cls.Name, iface, ownMethods, ownProps);
                    break;

                case TgmlClassDecl parentCls:
                    CheckAbstractMembers(ctx, file, cls.Name, parentCls, ownMethods, ownProps);
                    break;
            }
        }
    }

    // ── Interface contract ────────────────────────────────────────────────────

    private static void CheckInterface(TranspileContext ctx, TgmlFile file,
        string implName, TgmlInterfaceDecl iface,
        HashSet<string> methods, HashSet<string> props)
    {
        foreach (var m in iface.Methods)
            if (!methods.Contains(m.Name))
                ctx.AddError(
                    $"'{implName}' does not implement interface method '{iface.Name}.{m.Name}'.",
                    file.FileName);

        foreach (var p in iface.Properties)
            if (!props.Contains(p.Name))
                ctx.AddError(
                    $"'{implName}' does not implement interface property '{iface.Name}.{p.Name}'.",
                    file.FileName);

        // Recurse into base interfaces
        foreach (var baseRef in iface.BaseInterfaces)
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseIface)
                && baseIface is TgmlInterfaceDecl bi)
                CheckInterface(ctx, file, implName, bi, methods, props);
    }

    // ── Abstract member contract ──────────────────────────────────────────────

    private static void CheckAbstractMembers(TranspileContext ctx, TgmlFile file,
        string implName, TgmlClassDecl parent,
        HashSet<string> methods, HashSet<string> props)
    {
        foreach (var abs in CollectInheritedAbstractMethods(ctx, parent))
            if (!methods.Contains(abs))
                ctx.AddError(
                    $"Non-abstract class '{implName}' does not implement inherited abstract method '{abs}'.",
                    file.FileName);

        foreach (var abs in CollectInheritedAbstractProps(ctx, parent))
            if (!props.Contains(abs))
                ctx.AddError(
                    $"Non-abstract class '{implName}' does not implement inherited abstract property '{abs}'.",
                    file.FileName);
    }

    // ── Collection helpers ────────────────────────────────────────────────────

    /// <summary>
    ///     All method names that are concretely implemented in <paramref name="cls" /> or
    ///     any of its concrete ancestor classes.
    /// </summary>
    private static HashSet<string> CollectConcreteMethodNames(TranspileContext ctx, TgmlClassDecl cls)
    {
        var result = new HashSet<string>(
            cls.Methods.Where(m => !m.IsAbstract).Select(m => m.Name),
            StringComparer.Ordinal);

        foreach (var baseRef in cls.BaseTypes)
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var bd) && bd is TgmlClassDecl baseCls)
                foreach (var n in CollectConcreteMethodNames(ctx, baseCls))
                    result.Add(n);

        return result;
    }

    private static HashSet<string> CollectConcretePropNames(TranspileContext ctx, TgmlClassDecl cls)
    {
        var result = new HashSet<string>(
            cls.Properties
                .Where(p => p.Modifiers.Virtual != VirtualModifier.Abstract)
                .Select(p => p.Name),
            StringComparer.Ordinal);

        foreach (var baseRef in cls.BaseTypes)
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var bd) && bd is TgmlClassDecl baseCls)
                foreach (var n in CollectConcretePropNames(ctx, baseCls))
                    result.Add(n);

        return result;
    }

    private static HashSet<string> CollectInheritedAbstractMethods(TranspileContext ctx, TgmlClassDecl cls)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in cls.Methods)
        {
            if (m.IsAbstract) result.Add(m.Name);
            if (m.IsOverride) result.Remove(m.Name); // overridden in this class
        }

        foreach (var baseRef in cls.BaseTypes)
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var bd) && bd is TgmlClassDecl baseCls)
                foreach (var abs in CollectInheritedAbstractMethods(ctx, baseCls))
                    result.Add(abs);

        return result;
    }

    private static HashSet<string> CollectInheritedAbstractProps(TranspileContext ctx, TgmlClassDecl cls)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var p in cls.Properties)
        {
            if (p.Modifiers.Virtual == VirtualModifier.Abstract) result.Add(p.Name);
            if (p.Modifiers.Virtual == VirtualModifier.Override) result.Remove(p.Name);
        }

        foreach (var baseRef in cls.BaseTypes)
            if (ctx.TypeTable.TryResolve(baseRef.Name.Full, out var bd) && bd is TgmlClassDecl baseCls)
                foreach (var abs in CollectInheritedAbstractProps(ctx, baseCls))
                    result.Add(abs);

        return result;
    }
}

