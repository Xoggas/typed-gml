using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Transpiler.Visitor;

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
        var declarations = program.usingDecl().Cast<IParseTree>()
            .Concat(program.namespaceDecl())
            .Concat(program.topLevelDecl())
            .Select(Node)
            .ToList();
        return new FileNode(filePath, declarations, Location(program));
    }

    public override IAstNode VisitTopLevelDecl(TypedGMLParser.TopLevelDeclContext context) =>
        context.typeDecl() is null ? Node(context.functionDecl()!) : Node(context.typeDecl());

    public override IAstNode VisitTypeDecl(TypedGMLParser.TypeDeclContext context) =>
        Node(context.GetChild(0));

    public override IAstNode VisitMemberDecl(TypedGMLParser.MemberDeclContext context) =>
        Node(context.GetChild(0));

    public override IAstNode VisitInterfaceMemberDecl(TypedGMLParser.InterfaceMemberDeclContext context) =>
        Node(context.GetChild(0));

    public override IAstNode VisitStatement(TypedGMLParser.StatementContext context) =>
        Node(context.GetChild(0));

    private IAstNode Node(IParseTree node) => Visit(node)!;
    private IAstNode? MaybeNode(IParseTree? node) => node is null ? null : Visit(node)!;
    private SourceLocation Location(ParserRuleContext context) => Location(context.Start);
    private SourceLocation Location(IToken token) => new(_filePath, token.Line, token.Column);
    private string Text(IParseTree? node) => node?.GetText() ?? string.Empty;
    private IReadOnlyList<T> Nodes<T>(IEnumerable<IParseTree> nodes) where T : class, IAstNode => nodes.Select(n => (T)Node(n)).ToList();
    private IReadOnlyList<string> Texts(IEnumerable<IParseTree> nodes) => nodes.Select(Text).ToList();
    private IReadOnlyList<string> Parts(params IParseTree?[] nodes)
    {
        var parts = new List<string>();
        foreach (var node in nodes.Where(n => n is not null))
            if (!string.IsNullOrEmpty(node!.GetText()))
                parts.Add(node.GetText());
        return parts;
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
