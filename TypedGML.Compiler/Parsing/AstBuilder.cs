using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Visitor;

namespace TypedGML.Compiler.Parsing;

public sealed partial class AstBuilder(DiagnosticBag diagnostics) : TypedGMLBaseVisitor<IAstNode>
{
    private string _filePath = string.Empty;
    private IReadOnlyList<IToken> _tokens = [];

    public FileNode Build(string filePath)
    {
        _filePath = filePath;
        var listener = new ParseErrorListener(diagnostics, filePath);
        var lexer = new TypedGMLLexer(new AntlrFileStream(filePath));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        var rawStream = new CommonTokenStream(lexer);
        rawStream.Fill();
        _tokens = rawStream.GetTokens().ToList();
        var parserTokens = _tokens.Where(t => t.Type != TypedGMLParser.DOC_COMMENT).ToList();
        var parser = new TypedGMLParser(new CommonTokenStream(new ListTokenSource(parserTokens)));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);
        var program = parser.program();

        var usings = program.usingDecl().Select(MaybeNode).Where(n => n is not null).Cast<IAstNode>().ToList();
        var fileScopedNs = program.namespaceDecl().FirstOrDefault(ns => ns.SEMI() is not null);
        var topLevelNodes = program.topLevelDecl().Select(MaybeNode).Where(n => n is not null).Cast<IAstNode>().ToList();

        if (fileScopedNs is not null)
        {
            var nsNode = (NamespaceDeclarationNode)Node(fileScopedNs);
            var wrappedNs = nsNode with { Body = topLevelNodes };
            return new FileNode(filePath, usings.Append(wrappedNs).ToList(), Location(program));
        }

        var blockNs = program.namespaceDecl().Select(MaybeNode).Where(n => n is not null).Cast<IAstNode>().ToList();
        var declarations = usings.Concat(blockNs).Concat(topLevelNodes).ToList();
        return new FileNode(filePath, declarations, Location(program));
    }

    public override IAstNode VisitTopLevelDecl(TypedGMLParser.TopLevelDeclContext context) =>
        context.typeDecl() is not null ? Node(context.typeDecl()) :
        context.functionDecl() is not null ? Node(context.functionDecl()!) : null!;

    public override IAstNode VisitTypeDecl(TypedGMLParser.TypeDeclContext context) =>
        Node(context.GetChild(0));

    public override IAstNode VisitMemberDecl(TypedGMLParser.MemberDeclContext context) =>
        context.GetChild(0) is { } child ? (MaybeNode(child) ?? null!) : null!;

    public override IAstNode VisitInterfaceMemberDecl(TypedGMLParser.InterfaceMemberDeclContext context) =>
        Node(context.GetChild(0));

    public override IAstNode VisitStatement(TypedGMLParser.StatementContext context) =>
        Node(context.GetChild(0));

    private IAstNode Node(IParseTree node) => Visit(node)!;
    private IAstNode? MaybeNode(IParseTree? node) => node is null ? null : Visit(node)!;
    private SourceLocation Location(ParserRuleContext context) => Location(context.Start);
    private SourceLocation Location(IToken token) => new(_filePath, token.Line, token.Column);
    private string Text(IParseTree? node) => node?.GetText() ?? string.Empty;
    private IReadOnlyList<T> Nodes<T>(IEnumerable<IParseTree> nodes) where T : class, IAstNode => nodes.Select(MaybeNode).Where(n => n is T).Cast<T>().ToList();
    private IReadOnlyList<IAstNode> TypeMembers(IEnumerable<IParseTree> nodes, string typeName) =>
        Nodes<IAstNode>(nodes).Select(node => node is StaticConstructorDeclarationNode ctor ? ctor with { TypeName = typeName } : node).ToList();
    private IReadOnlyList<string> Texts(IEnumerable<IParseTree> nodes) => nodes.Select(Text).ToList();
    private IReadOnlyList<string> Parts(params IParseTree?[] nodes)
    {
        var parts = new List<string>();
        foreach (var node in nodes.Where(n => n is not null))
            CollectParts(node!, parts);
        return parts;
    }

    private static void CollectParts(IParseTree node, List<string> parts)
    {
        if (node.ChildCount == 0)
        {
            if (!string.IsNullOrEmpty(node.GetText()))
                parts.Add(node.GetText());
            return;
        }

        for (var i = 0; i < node.ChildCount; i++)
            CollectParts(node.GetChild(i), parts);
    }

    private DocCommentNode? Doc(ParserRuleContext context)
    {
        var index = context.Start.TokenIndex - 1;
        var tokens = new List<IToken>();
        while (index >= 0 && _tokens[index].Type == TypedGMLParser.DOC_COMMENT) tokens.Add(_tokens[index--]);
        if (tokens.Count == 0) return null;
        tokens.Reverse();
        return new DocCommentNode(tokens.Select(t => t.Text).ToList(), Location(tokens[0]));
    }

    private IReadOnlyList<GenericParamNode> GenericParams(TypedGMLParser.TypeParamsContext? context) =>
        context is null ? [] : Nodes<GenericParamNode>(context.typeParam());

    private IReadOnlyList<ParameterNode> Parameters(TypedGMLParser.ParamListContext? context) =>
        context is null ? [] : Nodes<ParameterNode>(context.param());

    private IReadOnlyList<DecoratorNode> Decorators(IEnumerable<TypedGMLParser.DecoratorContext> contexts) =>
        Nodes<DecoratorNode>(contexts);

    private (IReadOnlyList<IAstNode> PositionalArgs, IReadOnlyList<NamedArgNode> NamedArgs) Args(TypedGMLParser.ArgListContext? context)
    {
        var positional = new List<IAstNode>();
        var named = new List<NamedArgNode>();
        foreach (var arg in context?.arg() ?? [])
            if (arg.nameId() is null) positional.Add(Node(arg.expression()));
            else named.Add(new NamedArgNode(Text(arg.nameId()), Node(arg.expression()), Location(arg)));
        return (positional, named);
    }

    private IReadOnlyList<IAstNode> DecoratorArgs(TypedGMLParser.ArgListContext? context) =>
        (context?.arg() ?? []).Select(arg => arg.nameId() is null
            ? Node(arg.expression())
            : new NamedArgNode(Text(arg.nameId()), Node(arg.expression()), Location(arg))).ToList();

    private IReadOnlyList<string> NameofChain(TypedGMLParser.ExpressionContext context) => context switch
    {
        TypedGMLParser.IdExprContext id => [id.ID().GetText()],
        TypedGMLParser.FieldAccessExprContext field => NameofChain(field.expression()).Concat([Text(field.nameId())]).ToList(),
        TypedGMLParser.BaseAccessExprContext access => [access.BASE().GetText(), Text(access.nameId())],
        _ => [context.GetText()]
    };

    private static string Unquote(string text) =>
        text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;
}
