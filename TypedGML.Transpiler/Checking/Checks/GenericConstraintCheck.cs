using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 3: For every generic type instantiation, verifies that each supplied type argument
///     satisfies the corresponding type parameter's constraint (<c>T : IFoo</c>).
///     <para>
///         Constraint satisfaction is transitive — a type satisfies a constraint if it, or any of
///         its base types / interfaces, matches the constraint type.
///     </para>
///     <para>
///         If a type argument is itself an unresolved type variable (a type param of the containing
///         declaration), the constraint check is skipped for that position.
///     </para>
/// </summary>
public sealed class GenericConstraintCheck : IAtomicCheck
{
    public string Name => "GenericConstraintCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            CheckTypeDecl(context, file, type);
        }
    }

    // ── traversal ───────────────────────────────────────────────────────────

    private static void CheckTypeDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                foreach (var bt in cls.BaseTypes)
                {
                    CheckTypeRef(ctx, file, bt, decl.TypeParams);
                }

                foreach (var field in cls.Fields)
                {
                    CheckTypeRef(ctx, file, field.Type, decl.TypeParams);
                }

                foreach (var prop in cls.Properties)
                {
                    CheckTypeRef(ctx, file, prop.Type, decl.TypeParams);
                }

                foreach (var method in cls.Methods)
                {
                    CheckMethod(ctx, file, method, decl.TypeParams);
                }

                if (cls.Constructor is not null)
                {
                    foreach (var p in cls.Constructor.Params)
                    {
                        CheckTypeRef(ctx, file, p.Type, decl.TypeParams);
                    }
                }

                foreach (var nested in cls.NestedTypes)
                {
                    CheckTypeDecl(ctx, file, nested);
                }

                break;

            case TgmlStructDecl str:
                foreach (var bt in str.BaseTypes)
                {
                    CheckTypeRef(ctx, file, bt, decl.TypeParams);
                }

                foreach (var field in str.Fields)
                {
                    CheckTypeRef(ctx, file, field.Type, decl.TypeParams);
                }

                foreach (var prop in str.Properties)
                {
                    CheckTypeRef(ctx, file, prop.Type, decl.TypeParams);
                }

                foreach (var method in str.Methods)
                {
                    CheckMethod(ctx, file, method, decl.TypeParams);
                }

                if (str.Constructor is not null)
                {
                    foreach (var p in str.Constructor.Params)
                    {
                        CheckTypeRef(ctx, file, p.Type, decl.TypeParams);
                    }
                }

                foreach (var nested in str.NestedTypes)
                {
                    CheckTypeDecl(ctx, file, nested);
                }

                break;

            case TgmlInterfaceDecl iface:
                foreach (var bi in iface.BaseInterfaces)
                {
                    CheckTypeRef(ctx, file, bi, decl.TypeParams);
                }

                foreach (var method in iface.Methods)
                {
                    var visible = decl.TypeParams.Concat(method.TypeParams).ToList();
                    CheckTypeRef(ctx, file, method.ReturnType, visible);
                    foreach (var p in method.Params)
                    {
                        CheckTypeRef(ctx, file, p.Type, visible);
                    }
                }

                foreach (var prop in iface.Properties)
                {
                    CheckTypeRef(ctx, file, prop.Type, decl.TypeParams);
                }

                break;
        }
    }

    private static void CheckMethod(TranspileContext ctx, TgmlFile file,
        TgmlMethodDecl method, List<TgmlTypeParam> ownerTypeParams)
    {
        var visible = ownerTypeParams.Concat(method.TypeParams).ToList();
        CheckTypeRef(ctx, file, method.ReturnType, visible);
        foreach (var p in method.Params)
        {
            CheckTypeRef(ctx, file, p.Type, visible);
        }
    }

    // ── core check ──────────────────────────────────────────────────────────

    private static void CheckTypeRef(TranspileContext ctx, TgmlFile file,
        TgmlTypeRef typeRef, IEnumerable<TgmlTypeParam> visibleTypeParams)
    {
        var visible = visibleTypeParams as List<TgmlTypeParam> ?? visibleTypeParams.ToList();

        // Recurse into nested type arguments
        foreach (var arg in typeRef.TypeArgs)
        {
            CheckTypeRef(ctx, file, arg, visible);
        }

        // Only check instantiations that actually supply type arguments
        if (typeRef.TypeArgs.Count == 0)
        {
            return;
        }

        if (!ctx.TypeTable.TryResolve(typeRef.Name.Full, out var resolvedDecl) || resolvedDecl is null)
        {
            return;
        }

        var typeParams = resolvedDecl.TypeParams;

        for (var i = 0; i < Math.Min(typeRef.TypeArgs.Count, typeParams.Count); i++)
        {
            var constraint = typeParams[i].Constraint;
            if (constraint is null)
            {
                continue;
            }

            var argRef = typeRef.TypeArgs[i];

            // If the arg is an unresolved type variable, we cannot check it statically
            if (visible.Any(tp => tp.Name == argRef.Name.Full))
            {
                continue;
            }

            if (!ctx.TypeTable.TryResolve(argRef.Name.Full, out var argDecl) || argDecl is null)
            {
                continue; // unknown type — already reported by InheritanceCheck
            }

            if (!SatisfiesConstraint(argDecl, constraint, ctx))
            {
                ctx.AddError(
                    $"Type argument '{argRef.Name.Full}' does not satisfy the constraint " +
                    $"'{constraint}' on type parameter '{typeParams[i].Name}' " +
                    $"of '{resolvedDecl.QualifiedName ?? resolvedDecl.Name}'.",
                    file.FileName);
            }
        }
    }

    // ── constraint satisfaction ──────────────────────────────────────────────

    /// <summary>
    ///     Returns true if <paramref name="argDecl" /> itself, or any of its base types, matches
    ///     the constraint's type name (shallow name or qualified name).
    /// </summary>
    private static bool SatisfiesConstraint(TgmlTypeDecl argDecl, TgmlTypeRef constraint,
        TranspileContext ctx)
    {
        var constraintName = constraint.Name.Full;
        var allBases = CollectAllBaseNames(argDecl, ctx, new HashSet<string>(StringComparer.Ordinal));

        return allBases.Contains(constraintName)
               || argDecl.QualifiedName == constraintName
               || argDecl.Name == constraintName;
    }

    /// <summary>
    ///     Recursively collects the qualified name (or simple name) of every base type /
    ///     interface in the inheritance hierarchy of <paramref name="decl" />.
    /// </summary>
    private static HashSet<string> CollectAllBaseNames(TgmlTypeDecl decl, TranspileContext ctx,
        HashSet<string> visited)
    {
        var qn = decl.QualifiedName ?? decl.Name;
        if (!visited.Add(qn))
        {
            return visited; // cycle guard
        }

        var bases = decl switch
        {
            TgmlClassDecl c => c.BaseTypes,
            TgmlStructDecl s => s.BaseTypes,
            TgmlInterfaceDecl i => i.BaseInterfaces,
            _ => null
        };

        if (bases is null)
        {
            return visited;
        }

        foreach (var b in bases)
        {
            visited.Add(b.Name.Full);
            if (ctx.TypeTable.TryResolve(b.Name.Full, out var baseDecl) && baseDecl is not null)
            {
                CollectAllBaseNames(baseDecl, ctx, visited);
            }
        }

        return visited;
    }
}