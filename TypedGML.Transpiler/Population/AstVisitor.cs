using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

/// <summary>
///     Walks the ANTLR parse tree and converts it into typed AST model objects.
///     Split across five partial files:
///     <list type="bullet">
///         <item><c>AstVisitor.cs</c> — helpers and modifiers</item>
///         <item><c>AstVisitor.TypeDecls.cs</c> — type declarations</item>
///         <item><c>AstVisitor.Members.cs</c> — member declarations</item>
///         <item><c>AstVisitor.Statements.cs</c> — statement visitors</item>
///         <item><c>AstVisitor.Expressions.cs</c> — expression visitors</item>
///     </list>
/// </summary>
public sealed partial class AstVisitor : TypedGMLBaseVisitor<object?>
{
    // ── Name helpers ──────────────────────────────────────────────────────────

    private TgmlQualifiedName QName(TypedGMLParser.QualifiedNameContext? ctx)
    {
        if (ctx is null) return new TgmlQualifiedName { Parts = ["?"] };
        return new TgmlQualifiedName { Parts = ctx.ID().Select(t => t.GetText()).ToList() };
    }

    /// <summary>Returns the text of a nameId context (ID or contextual keyword).</summary>
    private static string NameId(TypedGMLParser.NameIdContext? ctx) => ctx?.GetText() ?? string.Empty;

    private TgmlTypeRef TypeRef(TypedGMLParser.TypeRefContext? ctx)
    {
        if (ctx is null) return new TgmlTypeRef { Name = new TgmlQualifiedName { Parts = ["?"] } };
        var name = QName(ctx.qualifiedName());
        var args = ctx.typeArgs() is { } ta ? TypeArgsList(ta) : new List<TgmlTypeRef>();
        return new TgmlTypeRef { Name = name, TypeArgs = args, ArrayDepth = ctx.LBRACKET().Length };
    }

    private List<TgmlTypeRef> TypeArgsList(TypedGMLParser.TypeArgsContext ctx)
        => ctx.typeRef().Select(t => TypeRef(t)).ToList();

    private List<TgmlTypeRef> InheritanceList(TypedGMLParser.InheritanceListContext? ctx)
        => ctx is null ? [] : ctx.typeRef().Select(t => TypeRef(t)).ToList();

    private List<TgmlTypeParam> TypeParams(TypedGMLParser.TypeParamsContext? ctx)
    {
        if (ctx is null) return [];
        return ctx.typeParam().Select(tp => new TgmlTypeParam
        {
            Name = tp.ID().GetText(),
            Constraint = tp.typeRef() is { } tr ? TypeRef(tr) : null
        }).ToList();
    }

    // ── Modifier parsers ──────────────────────────────────────────────────────

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
        return new FieldModifiers(access, ctx.STATIC() is not null, ctx.READONLY() is not null,
            ctx.CONST() is not null);
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

        IToken? virtTok = null;
        for (var i = 0; i < ctx.ChildCount; i++)
        {
            var t = ctx.GetChild(i).GetText();
            if (t is "virtual" or "abstract" or "override" or "sealed")
            {
                virtTok = (ctx.GetChild(i) as ITerminalNode)?.Symbol;
                break;
            }
        }

        return new PropertyModifiers(access, scope, virtTok?.Text switch
        {
            "virtual" => VirtualModifier.Virtual,
            "abstract" => VirtualModifier.Abstract,
            "override" => VirtualModifier.Override,
            "sealed" => VirtualModifier.Sealed,
            _ => VirtualModifier.None
        });
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
            var t = ctx.GetChild(i).GetText();
            if (t is "virtual" or "abstract" or "override" or "sealed")
            {
                virtTok = (ctx.GetChild(i) as ITerminalNode)?.Symbol;
                break;
            }
        }

        return new MethodModifiers(access, isStatic, virtTok?.Text switch
        {
            "virtual" => VirtualModifier.Virtual,
            "abstract" => VirtualModifier.Abstract,
            "override" => VirtualModifier.Override,
            "sealed" => VirtualModifier.Sealed,
            _ => VirtualModifier.None
        });
    }

    // ── Collection helpers ────────────────────────────────────────────────────

    private List<TgmlDecorator> Decorators(TypedGMLParser.DecoratorContext[] ctxs)
        => ctxs.Select(d => (TgmlDecorator)Visit(d)!).ToList();

    private List<TgmlParam> ParamList(TypedGMLParser.ParamListContext? ctx)
        => ctx is null ? [] : ctx.param().Select(p => (TgmlParam)Visit(p)!).ToList();

    private List<TgmlArgument> ArgList(TypedGMLParser.ArgListContext? ctx)
        => ctx is null ? [] : ctx.arg().Select(a => (TgmlArgument)Visit(a)!).ToList();

    private static int Line(ParserRuleContext ctx) => ctx.Start.Line;
    private static int Column(ParserRuleContext ctx) => ctx.Start.Column;

    // ── Pass-throughs ─────────────────────────────────────────────────────────

    public override object? VisitQualifiedName([NotNull] TypedGMLParser.QualifiedNameContext ctx)
        => QName(ctx);

    public override object? VisitTypeRef([NotNull] TypedGMLParser.TypeRefContext ctx)
        => TypeRef(ctx);
}

