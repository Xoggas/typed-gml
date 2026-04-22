using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public abstract partial class AstBodyWalker
{
    private void WalkType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        OnEnterType(ctx, file, decl);

        switch (decl)
        {
            case TgmlClassDecl cls:
                WalkClassMembers(ctx, file, cls);
                foreach (var nested in cls.NestedTypes)
                    WalkType(ctx, file, nested);
                break;
            case TgmlStructDecl str:
                WalkStructMembers(ctx, file, str);
                foreach (var nested in str.NestedTypes)
                    WalkType(ctx, file, nested);
                break;
            case TgmlInterfaceDecl iface:
                WalkInterfaceMembers(ctx, file, iface);
                break;
        }

        OnLeaveType(ctx, file, decl);
    }

    private void WalkClassMembers(TranspileContext ctx, TgmlFile file, TgmlClassDecl cls)
    {
        foreach (var field in cls.Fields)
        {
            if (field.Initializer is not null)
                WalkExpr(ctx, file, field.Initializer, new WalkContext { OwnerType = cls, Member = field });
        }

        WalkConstructor(ctx, file, cls, cls.Constructor);
        WalkMethods(ctx, file, cls, cls.Methods);

        foreach (var prop in cls.Properties)
            WalkProperty(ctx, file, cls, prop);
    }

    private void WalkStructMembers(TranspileContext ctx, TgmlFile file, TgmlStructDecl str)
    {
        foreach (var field in str.Fields)
        {
            if (field.Initializer is not null)
                WalkExpr(ctx, file, field.Initializer, new WalkContext { OwnerType = str, Member = field });
        }

        WalkConstructor(ctx, file, str, str.Constructor);
        WalkMethods(ctx, file, str, str.Methods);

        foreach (var prop in str.Properties)
            WalkProperty(ctx, file, str, prop);
    }

    private void WalkInterfaceMembers(TranspileContext ctx, TgmlFile file, TgmlInterfaceDecl iface)
    {
        foreach (var method in iface.Methods.Where(m => m.Body is not null))
        {
            var wctx = new WalkContext
            {
                OwnerType = iface,
                ReturnTypeName = DefaultExpressionFacts.DescribeType(method.ReturnType),
                Params = method.Params
            };
            OnEnterCallable(ctx, file, wctx);
            WalkBlock(ctx, file, method.Body!, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }
    }

    private void WalkConstructor(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlConstructorDecl? ctor)
    {
        if (ctor is null)
            return;

        var wctx = new WalkContext { OwnerType = owner, Member = ctor, InConstructor = true, Params = ctor.Params };
        OnEnterCallable(ctx, file, wctx);

        foreach (var arg in ctor.BaseArgs ?? [])
            WalkExpr(ctx, file, arg.Value, wctx);

        WalkBlock(ctx, file, ctor.Body, wctx);
        OnLeaveCallable(ctx, file, wctx);
    }

    private void WalkMethods(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, IEnumerable<TgmlMethodDecl> methods)
    {
        foreach (var method in methods)
        {
            var wctx = new WalkContext
            {
                OwnerType = owner,
                Member = method,
                ReturnTypeName = DefaultExpressionFacts.DescribeType(method.ReturnType),
                Params = method.Params
            };

            OnEnterCallable(ctx, file, wctx);
            foreach (var param in method.Params)
            {
                if (param.Default is not null)
                    WalkExpr(ctx, file, param.Default, wctx);
            }

            if (method.Body is not null)
                WalkBlock(ctx, file, method.Body, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }
    }

    private void WalkProperty(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlPropertyDecl prop)
    {
        if (prop.Getter?.Body is { } getterBody)
        {
            var getterParams = prop.IndexParam is not null ? [prop.IndexParam] : Array.Empty<TgmlParam>();
            var getterContext = new WalkContext
            {
                OwnerType = owner,
                Member = prop,
                ReturnTypeName = DefaultExpressionFacts.DescribeType(prop.Type),
                Params = getterParams
            };
            OnEnterCallable(ctx, file, getterContext);
            WalkBlock(ctx, file, getterBody, getterContext);
            OnLeaveCallable(ctx, file, getterContext);
        }

        if (prop.Setter?.Body is not { } setterBody)
            return;

        var setterParams = new List<TgmlParam>();
        if (prop.IndexParam is not null)
            setterParams.Add(prop.IndexParam);
        setterParams.Add(new TgmlParam { Name = "value", Type = prop.Type });

        var setterContext = new WalkContext { OwnerType = owner, Member = prop, Params = setterParams };
        OnEnterCallable(ctx, file, setterContext);
        WalkBlock(ctx, file, setterBody, setterContext);
        OnLeaveCallable(ctx, file, setterContext);
    }
}
