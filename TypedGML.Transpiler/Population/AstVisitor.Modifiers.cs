using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    private AccessModifier AccessMod(TypedGMLParser.AccessModContext ctx) =>
        ctx.GetText() switch
        {
            "public" => AccessModifier.Public,
            "protected" => AccessModifier.Protected,
            _ => AccessModifier.Private
        };

    private ClassModifier ClassMod(TypedGMLParser.ClassModContext? ctx) =>
        ctx?.GetText() switch
        {
            "abstract" => ClassModifier.Abstract,
            "sealed" => ClassModifier.Sealed,
            "virtual" => ClassModifier.Virtual,
            "static" => ClassModifier.Virtual,
            _ => ClassModifier.None
        };

    private ScopeModifier ScopeMod(TypedGMLParser.ScopeModContext? ctx) =>
        ctx?.GetText() switch
        {
            "static" => ScopeModifier.Static,
            _ => ScopeModifier.None
        };

    private static VirtualModifier VirtualMod(IToken? tok) =>
        tok?.Text switch
        {
            "virtual" => VirtualModifier.Virtual,
            "abstract" => VirtualModifier.Abstract,
            "override" => VirtualModifier.Override,
            "sealed" => VirtualModifier.Sealed,
            _ => VirtualModifier.None
        };

    private static FieldModifiers ParseFieldModifiers(TypedGMLParser.FieldModifiersContext ctx)
    {
        var access = ctx.accessMod() is { } am
            ? am.GetText() switch
            {
                "public" => AccessModifier.Public,
                "protected" => AccessModifier.Protected,
                _ => AccessModifier.Private
            }
            : AccessModifier.Private;

        return new FieldModifiers(access, ctx.STATIC() is not null, ctx.READONLY() is not null, ctx.CONST() is not null);
    }

    private static PropertyModifiers ParsePropertyModifiers(TypedGMLParser.PropertyModifiersContext ctx)
    {
        var access = ctx.accessMod() is { } am
            ? am.GetText() switch
            {
                "public" => AccessModifier.Public,
                "protected" => AccessModifier.Protected,
                _ => AccessModifier.Private
            }
            : AccessModifier.Private;
        var scope = ctx.scopeMod() is not null ? ScopeModifier.Static : ScopeModifier.None;
        var isReadonly = false;
        IToken? virtTok = null;

        for (var i = 0; i < ctx.ChildCount; i++)
        {
            var text = ctx.GetChild(i).GetText();
            if (text == "readonly")
            {
                isReadonly = true;
                continue;
            }

            if (text is "virtual" or "abstract" or "override" or "sealed")
            {
                virtTok = (ctx.GetChild(i) as ITerminalNode)?.Symbol;
                break;
            }
        }

        return new PropertyModifiers(access, scope, VirtualMod(virtTok), isReadonly);
    }

    private static MethodModifiers ParseMethodModifiers(TypedGMLParser.MethodModifiersContext ctx)
    {
        var access = ctx.accessMod() is { } am
            ? am.GetText() switch
            {
                "public" => AccessModifier.Public,
                "protected" => AccessModifier.Protected,
                _ => AccessModifier.Private
            }
            : AccessModifier.Private;
        var isStatic = ctx.STATIC() is not null;
        IToken? virtTok = null;

        for (var i = 0; i < ctx.ChildCount; i++)
        {
            var text = ctx.GetChild(i).GetText();
            if (text is "virtual" or "abstract" or "override" or "sealed")
            {
                virtTok = (ctx.GetChild(i) as ITerminalNode)?.Symbol;
                break;
            }
        }

        return new MethodModifiers(access, isStatic, VirtualMod(virtTok));
    }
}
