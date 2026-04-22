using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates default parameter values. They must be compile-time constants and
///     assignable to the declared parameter type.
/// </summary>
public sealed class DefaultParameterValueCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckTypeDecl(context, file, type);
    }

    private static void CheckTypeDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                foreach (var ctor in cls.Constructors)
                    CheckParameters(ctx, file, decl, ctor.Params);

                foreach (var method in cls.Methods)
                    CheckParameters(ctx, file, decl, method.Params);

                foreach (var nested in cls.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                foreach (var ctor in str.Constructors)
                    CheckParameters(ctx, file, decl, ctor.Params);

                foreach (var method in str.Methods)
                    CheckParameters(ctx, file, decl, method.Params);

                foreach (var nested in str.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlInterfaceDecl iface:
                foreach (var method in iface.Methods)
                    CheckParameters(ctx, file, decl, method.Params);
                break;
        }
    }

    private static void CheckParameters(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeDecl owner,
        IEnumerable<TgmlParam> parameters)
    {
        var checker = new ExprChecker(ctx, file, new SymbolTable(), owner);

        var seenDefault = false;
        foreach (var parameter in parameters)
        {
            if (parameter.Default is null)
            {
                if (seenDefault)
                {
                    ctx.AddError(
                        $"Non-default parameter '{parameter.Name}' cannot follow a parameter with a default value.",
                        file.FileName);
                }
                continue;
            }

            seenDefault = true;

            DefaultExpressionFacts.TryApplyContextualType(parameter.Default, DefaultExpressionFacts.DescribeType(parameter.Type));

            if (!ConstantExpressionFacts.IsCompileTimeConstant(parameter.Default))
            {
                ctx.AddError(
                    $"Default value for parameter '{parameter.Name}' must be a compile-time constant. Only literals and constant expressions are supported.",
                    file.FileName,
                    parameter.Default.Line,
                    parameter.Default.Column);
                continue;
            }

            var inferredType = checker.InferType(parameter.Default);
            if (inferredType is not null &&
                !TypeCompatibility.AreAssignable(parameter.Type.Name.Full, inferredType))
            {
                ctx.AddError(
                    $"Default value for parameter '{parameter.Name}' is of type '{inferredType}', which is not assignable to '{parameter.Type.Name.Full}'.",
                    file.FileName,
                    parameter.Default.Line,
                    parameter.Default.Column);
            }
        }
    }
}
