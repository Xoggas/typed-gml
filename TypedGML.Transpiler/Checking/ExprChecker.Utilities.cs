using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    public void CheckAssignCompatibility(string targetType, TgmlExpression valueExpr, int line, int col,
        string messageTemplate)
    {
        DefaultExpressionFacts.TryApplyContextualType(valueExpr, targetType);

        // InferType already recurses into all sub-expressions and fires any operand errors.
        var inferred = InferType(valueExpr);
        if (!CanConvertExpression(targetType, valueExpr, allowExplicit: false, apply: true) && inferred is not null)
            Error(line, col, messageTemplate.Replace("{1}", inferred));
    }

    private void Error(TgmlExpression expr, string msg) =>
        _ctx.AddError(msg, _file.FileName, expr.Line, expr.Column);

    private void Error(int line, int col, string msg) =>
        _ctx.AddError(msg, _file.FileName, line, col);

    private void DefineImplicitLocal(string name, TgmlExpression? initializer, int line, int col, string kind)
    {
        if (initializer is null)
        {
            Error(line, col, $"Implicitly typed {kind} '{name}' must have an initializer.");
            Symbols.Define(name, MakeTypeRef("any"));
            return;
        }

        var inferred = InferType(initializer);
        if (inferred is null || inferred == "null")
        {
            Error(line, col, $"Cannot infer the type of implicitly typed {kind} '{name}'.");
            Symbols.Define(name, MakeTypeRef("any"));
            return;
        }

        Symbols.Define(name, MakeTypeRef(inferred));
    }

    private static TgmlTypeRef MakeTypeRef(string name)
    {
        var trimmed = name.Trim();
        var arrayDepth = 0;
        while (trimmed.EndsWith("[]", StringComparison.Ordinal))
        {
            arrayDepth++;
            trimmed = trimmed[..^2];
        }

        var genericStart = trimmed.IndexOf('<');
        if (genericStart < 0)
        {
            return new TgmlTypeRef
            {
                Name = MakeQualifiedName(trimmed),
                ArrayDepth = arrayDepth
            };
        }

        var typeArgsPayload = trimmed[(genericStart + 1)..^1];
        return new TgmlTypeRef
        {
            Name = MakeQualifiedName(trimmed[..genericStart]),
            TypeArgs = SplitTopLevelTypeArgs(typeArgsPayload).Select(MakeTypeRef).ToList(),
            ArrayDepth = arrayDepth
        };
    }

    private static TgmlQualifiedName MakeQualifiedName(string name)
    {
        return new TgmlQualifiedName
        {
            Parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }

    private static List<string> SplitTopLevelTypeArgs(string payload)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < payload.Length; i++)
        {
            switch (payload[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(payload[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(payload[start..].Trim());
        return parts;
    }

    public void CheckBaseConstructorCall(TgmlConstructorDecl ctor)
    {
        foreach (var arg in ctor.BaseArgs ?? [])
            CheckExpr(arg.Value);

        if (_owner is not TgmlClassDecl ownerClass || ctor.BaseArgs is null)
        {
            // If base class has declared constructors but this ctor has no base() call, error.
            if (_owner is TgmlClassDecl ownerCls2 && ctor.BaseArgs is null)
            {
                var baseClass2 = ResolveBaseClass(ownerCls2);
                if (baseClass2 is not null && baseClass2.Constructors.Count > 0)
                    Error(ctor.Line, ctor.Column,
                        $"Constructor in '{ownerCls2.Name}' must call base constructor with 'base(...)'. Base class '{baseClass2.Name}' has {baseClass2.Constructors.Count} constructor(s).");
            }
            return;
        }

        var baseClass = ResolveBaseClass(ownerClass);
        if (baseClass is null || baseClass.Constructors.Count == 0)
            return;

        if (!CallArgumentBinder.TryResolveOverload(
                baseClass.Constructors,
                c => c.Params,
                ctor.BaseArgs,
                InferType,
                CanAssignImplicitly,
                out var resolvedCtor,
                out var bound,
                out var error))
        {
            var line = ctor.BaseArgs.FirstOrDefault()?.Value.Line ?? 0;
            var column = ctor.BaseArgs.FirstOrDefault()?.Value.Column ?? 0;
            Error(line, column, $"No base constructor overload matches the supplied arguments: {error}");
            return;
        }

        ctor.Metadata["NormalizedBaseArgs"] = bound!.Arguments.ToList();
        ApplyBoundArgumentConversions(bound.Parameters, bound.Arguments);
        ctor.Metadata["ResolvedBaseConstructor"] = resolvedCtor!;
    }

}
