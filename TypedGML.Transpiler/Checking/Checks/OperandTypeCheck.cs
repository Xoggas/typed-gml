using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Validates operator operand types, assignment compatibility, and
///     return-expression types by delegating to <see cref="ExprChecker" />.
/// </summary>
public sealed class OperandTypeCheck : IAtomicCheck
{
    public string Name => "OperandTypeCheck";

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckTypeDecl(context, file, type);
    }

    // ── Type-declaration traversal ────────────────────────────────────────────

    private static void CheckTypeDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                foreach (var field in cls.Fields)
                    if (field.Initializer is not null)
                        MakeChecker(ctx, file, cls).CheckAssignCompatibility(
                            DefaultExpressionFacts.DescribeType(field.Type),
                            field.Initializer,
                            field.Initializer.Line,
                            field.Initializer.Column,
                            $"Cannot assign '{{1}}' to field of type '{field.Type.Name.Full}'.");

                foreach (var ctor in cls.Constructors)
                    CheckConstructor(ctx, file, cls, ctor);

                foreach (var method in cls.Methods)
                    CheckMethod(ctx, file, cls, method);

                foreach (var prop in cls.Properties)
                    CheckProperty(ctx, file, cls, prop);

                foreach (var nested in cls.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                foreach (var field in str.Fields)
                    if (field.Initializer is not null)
                        MakeChecker(ctx, file, str).CheckAssignCompatibility(
                            DefaultExpressionFacts.DescribeType(field.Type),
                            field.Initializer,
                            field.Initializer.Line,
                            field.Initializer.Column,
                            $"Cannot assign '{{1}}' to field of type '{field.Type.Name.Full}'.");

                foreach (var ctor in str.Constructors)
                    CheckConstructor(ctx, file, str, ctor);

                foreach (var method in str.Methods)
                    CheckMethod(ctx, file, str, method);

                foreach (var prop in str.Properties)
                    CheckProperty(ctx, file, str, prop);

                foreach (var nested in str.NestedTypes)
                    CheckTypeDecl(ctx, file, nested);
                break;

            case TgmlInterfaceDecl iface:
                foreach (var method in iface.Methods.Where(m => m.Body is not null))
                {
                    var checker = MakeChecker(ctx, file, iface, DefaultExpressionFacts.DescribeType(method.ReturnType));
                    foreach (var p in method.Params) checker.Symbols.Define(p.Name, p.Type);
                    checker.CheckBlock(method.Body!);
                }

                break;
        }
    }

    private static void CheckConstructor(TranspileContext ctx, TgmlFile file,
        TgmlTypeDecl owner, TgmlConstructorDecl ctor)
    {
        var checker = MakeChecker(ctx, file, owner);
        foreach (var p in ctor.Params) checker.Symbols.Define(p.Name, p.Type);
        checker.CheckBaseConstructorCall(ctor);
        checker.CheckBlock(ctor.Body);
    }

    private static void CheckMethod(TranspileContext ctx, TgmlFile file,
        TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        if (method.Body is null) return;
        var checker = MakeChecker(ctx, file, owner, DefaultExpressionFacts.DescribeType(method.ReturnType));
        foreach (var p in method.Params) checker.Symbols.Define(p.Name, p.Type);
        checker.CheckBlock(method.Body);
    }

    private static void CheckProperty(TranspileContext ctx, TgmlFile file,
        TgmlTypeDecl owner, TgmlPropertyDecl prop)
    {
        if (prop.Getter?.Body is { } getterBody)
        {
            var checker = MakeChecker(ctx, file, owner, DefaultExpressionFacts.DescribeType(prop.Type));
            if (prop.IndexParam is not null)
                checker.Symbols.Define(prop.IndexParam.Name, prop.IndexParam.Type);
            checker.CheckBlock(getterBody);
        }

        if (prop.Setter?.Body is { } setterBody)
        {
            var checker = MakeChecker(ctx, file, owner);
            if (prop.IndexParam is not null)
                checker.Symbols.Define(prop.IndexParam.Name, prop.IndexParam.Type);
            checker.Symbols.Define("value", prop.Type);
            checker.CheckBlock(setterBody);
        }
    }

    private static ExprChecker MakeChecker(TranspileContext ctx, TgmlFile file,
        TgmlTypeDecl? owner = null, string returnType = "void")
        => new(ctx, file, new SymbolTable(), owner, returnType);
}
