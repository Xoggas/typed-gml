using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Visitor;

namespace TypedGML.Compiler.Parsing;

public sealed partial class AstBuilder
{
    public override IAstNode VisitNewObjectExpr(TypedGMLParser.NewObjectExprContext c) { var a = Args(c.argList()); return new ObjectCreationExpressionNode(Text(c.typeRef().qualifiedName()), c.typeRef().typeArgs() is null ? [] : Texts(c.typeRef().typeArgs().typeRef()), a.PositionalArgs, a.NamedArgs, Location(c)); }
    public override IAstNode VisitNullExpr(TypedGMLParser.NullExprContext c) => new LiteralExpressionNode(null, LiteralKind.Null, Location(c));
    public override IAstNode VisitCastExpr(TypedGMLParser.CastExprContext c) => new CastExpressionNode(Node(c.expression()), Text(c.typeRef()), CastKind.As, Location(c));
    public override IAstNode VisitFieldAccessExpr(TypedGMLParser.FieldAccessExprContext c) => new MemberAccessExpressionNode(Node(c.expression()), Text(c.nameId()), Location(c));
    public override IAstNode VisitArrayInitExpr(TypedGMLParser.ArrayInitExprContext c) => new ArrayLiteralExpressionNode(Nodes<IAstNode>(c.expression()), Location(c));
    public override IAstNode VisitTypeofExpr(TypedGMLParser.TypeofExprContext c) => new TypeofExpressionNode(Text(c.typeRef()), Location(c));
    public override IAstNode VisitRealExpr(TypedGMLParser.RealExprContext c) => new LiteralExpressionNode(c.realLiteral().GetText(), LiteralKind.Number, Location(c));
    public override IAstNode VisitBitwiseXor(TypedGMLParser.BitwiseXorContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitFuncCallExpr(TypedGMLParser.FuncCallExprContext c) { var a = Args(c.argList()); return new InvocationExpressionNode(new IdentifierExpressionNode(Text(c.nameId()), Location(c.nameId().Start)), a.PositionalArgs, a.NamedArgs, Location(c)); }
    public override IAstNode VisitBitwiseAnd(TypedGMLParser.BitwiseAndContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitParenExpr(TypedGMLParser.ParenExprContext c) => Node(c.expression());
    public override IAstNode VisitRightShiftExpr(TypedGMLParser.RightShiftExprContext c) => new BinaryExpressionNode(Node(c.expression(0)), ">>", Node(c.expression(1)), Location(c));
    public override IAstNode VisitDefaultOfExpr(TypedGMLParser.DefaultOfExprContext c) => new DefaultExpressionNode(Text(c.typeRef()), Location(c));
    public override IAstNode VisitStringExpr(TypedGMLParser.StringExprContext c) => new LiteralExpressionNode(Unquote(c.stringLiteral().GetText()), LiteralKind.String, Location(c));
    public override IAstNode VisitMethodCallExpr(TypedGMLParser.MethodCallExprContext c) { var a = Args(c.argList()); return new InvocationExpressionNode(new MemberAccessExpressionNode(Node(c.expression()), Text(c.nameId()), Location(c.nameId().Start)), a.PositionalArgs, a.NamedArgs, Location(c)); }
    public override IAstNode VisitUnaryExpr(TypedGMLParser.UnaryExprContext c) => new UnaryExpressionNode(c.GetChild(0).GetText(), Node(c.expression()), Location(c));
    public override IAstNode VisitBaseCallExpr(TypedGMLParser.BaseCallExprContext c) => new BaseCallExpressionNode(Text(c.nameId()), Args(c.argList()).PositionalArgs, Location(c));
    public override IAstNode VisitTernaryExpr(TypedGMLParser.TernaryExprContext c) => new TernaryExpressionNode(Node(c.expression(0)), Node(c.expression(1)), Node(c.expression(2)), Location(c));
    public override IAstNode VisitDictInitExpr(TypedGMLParser.DictInitExprContext c) => new DictionaryLiteralExpressionNode(Nodes<DictionaryEntryNode>(c.dictionaryEntry()), Location(c));
    public override IAstNode VisitBitwiseOr(TypedGMLParser.BitwiseOrContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitAssignExpr(TypedGMLParser.AssignExprContext c) => new AssignmentExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitIsExpr(TypedGMLParser.IsExprContext c) => new CastExpressionNode(Node(c.expression()), Text(c.typeRef()), CastKind.Is, Location(c));
    public override IAstNode VisitMulDivMod(TypedGMLParser.MulDivModContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitFieldKeywordExpr(TypedGMLParser.FieldKeywordExprContext c) => new IdentifierExpressionNode(c.FIELD().GetText(), Location(c));
    public override IAstNode VisitInvokeExpr(TypedGMLParser.InvokeExprContext c) { var a = Args(c.argList()); return new InvocationExpressionNode(Node(c.expression()), a.PositionalArgs, a.NamedArgs, Location(c)); }
    public override IAstNode VisitIntExpr(TypedGMLParser.IntExprContext c) => new LiteralExpressionNode(c.intLiteral().GetText(), LiteralKind.Number, Location(c));
    public override IAstNode VisitComparison(TypedGMLParser.ComparisonContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitBaseAccessExpr(TypedGMLParser.BaseAccessExprContext c) => new BaseAccessExpressionNode(Text(c.nameId()), Location(c));
    public override IAstNode VisitValueKeywordExpr(TypedGMLParser.ValueKeywordExprContext c) => new IdentifierExpressionNode(c.VALUE().GetText(), Location(c));
    public override IAstNode VisitLogicalAnd(TypedGMLParser.LogicalAndContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitAddSub(TypedGMLParser.AddSubContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitLambdaExprAtom(TypedGMLParser.LambdaExprAtomContext c) => Node(c.lambdaExpr());
    public override IAstNode VisitNameofExpr(TypedGMLParser.NameofExprContext c) => new NameofExpressionNode(NameofChain(c.expression()), Location(c));
    public override IAstNode VisitIndexExpr(TypedGMLParser.IndexExprContext c) => new IndexerAccessExpressionNode(Node(c.expression(0)), Node(c.expression(1)), Location(c));
    public override IAstNode VisitDefaultExpr(TypedGMLParser.DefaultExprContext c) => new IdentifierExpressionNode(c.DEFAULT().GetText(), Location(c));
    public override IAstNode VisitBoolExpr(TypedGMLParser.BoolExprContext c) => new LiteralExpressionNode(c.boolLiteral().GetText() == "true", LiteralKind.Bool, Location(c));
    public override IAstNode VisitLogicalOr(TypedGMLParser.LogicalOrContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitAsExpr(TypedGMLParser.AsExprContext c) => new CastExpressionNode(Node(c.expression()), Text(c.typeRef()), CastKind.As, Location(c));
    public override IAstNode VisitLeftShiftExpr(TypedGMLParser.LeftShiftExprContext c) => new BinaryExpressionNode(Node(c.expression(0)), c.GetChild(1).GetText(), Node(c.expression(1)), Location(c));
    public override IAstNode VisitIdExpr(TypedGMLParser.IdExprContext c) => new IdentifierExpressionNode(c.ID().GetText(), Location(c));
    public override IAstNode VisitLambdaExpr(TypedGMLParser.LambdaExprContext c) => new LambdaExpressionNode(c.paramList() is null ? [new ParameterNode(Text(c.nameId()), string.Empty, null, [], Location(c.nameId().Start))] : Parameters(c.paramList()), c.expression() is null ? Node(c.block()!) : Node(c.expression()), Location(c));
    public override IAstNode VisitDictionaryEntry(TypedGMLParser.DictionaryEntryContext c) => new DictionaryEntryNode(Node(c.expression(0)), Node(c.expression(1)), Location(c));
    public override IAstNode VisitIntLiteral(TypedGMLParser.IntLiteralContext c) => new LiteralExpressionNode(c.GetText(), LiteralKind.Number, Location(c));
    public override IAstNode VisitRealLiteral(TypedGMLParser.RealLiteralContext c) => new LiteralExpressionNode(c.GetText(), LiteralKind.Number, Location(c));
    public override IAstNode VisitStringLiteral(TypedGMLParser.StringLiteralContext c) => new LiteralExpressionNode(Unquote(c.GetText()), LiteralKind.String, Location(c));
    public override IAstNode VisitBoolLiteral(TypedGMLParser.BoolLiteralContext c) => new LiteralExpressionNode(c.GetText() == "true", LiteralKind.Bool, Location(c));
}
