using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 1: Ensures every argument of every decorator is a compile-time constant
///     (literal or unary negation/complement of a literal).
///     Decorators are metadata and are not evaluated at runtime, so expressions like
///     <c>@Object(new Game())</c> or <c>@NativeEvent(someVar)</c> are rejected.
/// </summary>
public sealed class DecoratorArgConstantCheck : IAtomicCheck
{

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckTypeDecl(context, file, type);
    }

    // -- Traversal -------------------------------------------------------------

    private static void CheckTypeDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        foreach (var decorator in decl.Decorators)
            CheckDecorator(ctx, file, decorator);

        switch (decl)
        {
            case TgmlClassDecl cls:
                CheckMembers(ctx, file, cls.Fields, cls.Properties, cls.Methods, cls.Constructor);
                foreach (var nested in cls.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                CheckMembers(ctx, file, str.Fields, str.Properties, str.Methods, str.Constructor);
                foreach (var nested in str.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlInterfaceDecl iface:
                foreach (var method in iface.Methods)
                    foreach (var decorator in method.Decorators)
                        CheckDecorator(ctx, file, decorator);
                foreach (var prop in iface.Properties)
                    foreach (var decorator in prop.Decorators)
                        CheckDecorator(ctx, file, decorator);
                break;
        }
    }

    private static void CheckMembers(
        TranspileContext ctx, TgmlFile file,
        IEnumerable<TgmlFieldDecl> fields,
        IEnumerable<TgmlPropertyDecl> properties,
        IEnumerable<TgmlMethodDecl> methods,
        TgmlConstructorDecl? constructor)
    {
        foreach (var field in fields)
            foreach (var decorator in field.Decorators)
                CheckDecorator(ctx, file, decorator);

        foreach (var prop in properties)
            foreach (var decorator in prop.Decorators)
                CheckDecorator(ctx, file, decorator);

        foreach (var method in methods)
            foreach (var decorator in method.Decorators)
                CheckDecorator(ctx, file, decorator);

        if (constructor is not null)
            foreach (var decorator in constructor.Decorators)
                CheckDecorator(ctx, file, decorator);
    }

    // -- Core check ------------------------------------------------------------

    private static void CheckDecorator(TranspileContext ctx, TgmlFile file, TgmlDecorator decorator)
    {
        foreach (var arg in decorator.Args)
        {
            if (!IsConstantExpr(arg.Value))
            {
                ctx.AddError(
                    $"Decorator '@{decorator.SimpleName}' argument must be a compile-time constant " +
                    $"(literal value). Non-constant expressions are not allowed in decorators.",
                    file.FileName, arg.Value.Line, arg.Value.Column);
            }
        }
    }

    private static bool IsConstantExpr(TgmlExpression expr) =>
        ConstantExpressionFacts.IsCompileTimeConstant(expr);
}

