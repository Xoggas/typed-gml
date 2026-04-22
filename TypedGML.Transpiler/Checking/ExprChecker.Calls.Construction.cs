using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private string VisitNewObject(TgmlNewObjectExpr no)
    {
        foreach (var a in no.Args) CheckExpr(a.Value);

        if (_ctx.TypeTable.TryResolve(no.Type.Name.Full, out var decl) && decl is TgmlClassDecl or TgmlStructDecl)
        {
            var constructors = decl switch
            {
                TgmlClassDecl cls => cls.Constructors.Cast<TgmlConstructorDecl>().ToList(),
                TgmlStructDecl str => str.Constructors.Cast<TgmlConstructorDecl>().ToList(),
                _ => []
            };

            if (constructors.Count == 0)
            {
                if (no.Args.Count > 0)
                    Error(no, $"Type '{no.Type.Name.Full}' does not define a constructor that accepts arguments.");
            }
            else if (!CallArgumentBinder.TryResolveOverload(
                         constructors,
                         c => c.Params,
                         no.Args,
                         InferType,
                         CanAssignImplicitly,
                         out _,
                         out var bound,
                         out var error))
            {
                Error(no, $"No constructor overload for '{no.Type.Name.Full}' matches the supplied arguments: {error}");
            }
            else
            {
                no.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
                ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
            }
        }

        return DefaultExpressionFacts.DescribeType(no.Type);
    }

    private string? VisitNewImplicit(TgmlNewImplicitExpr ni)
    {
        // Type must have been set by the assignment context via ApplyNewImplicitType
        if (!ni.Metadata.TryGetValue("InferredType", out var inf) || inf is not string typeName)
        {
            Error(ni, "Cannot use 'new()' without a type context. Specify the type explicitly.");
            return null;
        }

        foreach (var a in ni.Args) CheckExpr(a.Value);

        if (!_ctx.TypeTable.TryResolve(typeName, out var decl) || decl is null)
            return typeName;

        var constructors = decl switch
        {
            TgmlClassDecl cls => cls.Constructors.Cast<TgmlConstructorDecl>().ToList(),
            TgmlStructDecl str => str.Constructors.Cast<TgmlConstructorDecl>().ToList(),
            _ => []
        };

        if (constructors.Count == 0)
        {
            if (ni.Args.Count > 0)
                Error(ni, $"Type '{typeName}' does not define a constructor that accepts arguments.");
        }
        else if (!CallArgumentBinder.TryResolveOverload(
                     constructors, c => c.Params, ni.Args, InferType, CanAssignImplicitly,
                     out _, out var bound, out var error))
        {
            Error(ni, $"No constructor overload for '{typeName}' matches the supplied arguments: {error}");
        }
        else
        {
            ni.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
            ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        }

        return typeName;
    }

    private string? VisitBaseCall(TgmlBaseCallExpr bc)
    {
        foreach (var a in bc.Args) CheckExpr(a.Value);

        if (_owner is not TgmlClassDecl ownerClass)
            return null;

        var baseClass = ResolveBaseClass(ownerClass);
        if (baseClass is null)
            return null;

        var candidates = FindMethodsInHierarchy(baseClass, bc.MethodName);
        if (candidates.Count == 0)
        {
            if (bc.Args.Any(a => a.Name is not null))
                Error(bc, $"Named arguments cannot be used because base method '{bc.MethodName}' could not be resolved.");
            return null;
        }

        if (!CallArgumentBinder.TryResolveOverload(
                candidates,
                m => m.Params,
                bc.Args,
                InferType,
                CanAssignImplicitly,
                out var resolvedMethod,
                out var bound,
                out var error))
        {
            Error(bc, $"No base-method overload '{bc.MethodName}' matches the supplied arguments: {error}");
            return null;
        }

        bc.Metadata["NormalizedArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        return DefaultExpressionFacts.DescribeType(resolvedMethod!.ReturnType);
    }

    private string? VisitNewArray(TgmlNewArrayExpr na)
    {
        CheckExpr(na.Size);
        return null;
    }

}
