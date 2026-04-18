using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 3: Verifies that every generic type instantiation supplies exactly the right
///     number of type arguments.
///     e.g. if <c>List&lt;T&gt;</c> is declared, using it as <c>List&lt;int, string&gt;</c>
///     or bare <c>List</c> are both errors.
/// </summary>
public sealed class GenericArityCheck : IAtomicCheck
{
    public string Name => "GenericArityCheck";

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
        foreach (var tp in decl.TypeParams)
        {
            if (tp.Constraint is not null)
            {
                CheckTypeRef(ctx, file, tp.Constraint, decl.TypeParams);
            }
        }

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

                foreach (var ctor in cls.Constructors)
                foreach (var p in ctor.Params)
                    CheckTypeRef(ctx, file, p.Type, decl.TypeParams);

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

                foreach (var ctor in str.Constructors)
                foreach (var p in ctor.Params)
                    CheckTypeRef(ctx, file, p.Type, decl.TypeParams);

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

        foreach (var tp in method.TypeParams)
        {
            if (tp.Constraint is not null)
            {
                CheckTypeRef(ctx, file, tp.Constraint, visible);
            }
        }
    }

    // ── core check ──────────────────────────────────────────────────────────

    private static void CheckTypeRef(TranspileContext ctx, TgmlFile file,
        TgmlTypeRef typeRef, IEnumerable<TgmlTypeParam> visibleTypeParams)
    {
        var visible = visibleTypeParams as List<TgmlTypeParam> ?? visibleTypeParams.ToList();

        // Recurse into type arguments first
        foreach (var arg in typeRef.TypeArgs)
        {
            CheckTypeRef(ctx, file, arg, visible);
        }

        if (IsBuiltIn(typeRef.Name.Full))
        {
            return;
        }

        // If the name is one of the visible type parameters, skip (it is a type variable, not a concrete type)
        if (visible.Any(tp => tp.Name == typeRef.Name.Full))
        {
            return;
        }

        if (!ctx.TypeTable.TryResolve(typeRef.Name.Full, out var resolvedDecl) || resolvedDecl is null)
        {
            return; // unknown type — InheritanceCheck will have reported it
        }

        var expected = resolvedDecl.TypeParams.Count;
        var actual = typeRef.TypeArgs.Count;

        if (expected != actual)
        {
            ctx.AddError(
                $"Generic arity mismatch: '{resolvedDecl.QualifiedName ?? resolvedDecl.Name}' " +
                $"expects {expected} type argument(s) but {actual} were supplied.",
                file.FileName);
        }
    }

    private static bool IsBuiltIn(string name)
    {
        return name is "int" or "string" or "bool" or "void" or "object" or "any"
            or "real" or "number" or "array" or "struct" or "undefined";
    }
}