using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Visitor;

namespace TypedGML.Compiler.Parsing;

public sealed partial class AstBuilder
{
    public override IAstNode VisitBlock(TypedGMLParser.BlockContext context) =>
        new BlockStatementNode(Nodes<IAstNode>(context.statement()), Location(context));

    public override IAstNode VisitLocalVarDecl(TypedGMLParser.LocalVarDeclContext context) =>
        new VarDeclarationStatementNode(Text(context.nameId()), context.VAR() is null ? Text(context.typeRef()) : null, Node(context.expression()), context.VAR() is not null, Location(context));

    public override IAstNode VisitExpressionStmt(TypedGMLParser.ExpressionStmtContext context) =>
        new ExpressionStatementNode(Node(context.expression()), Location(context));

    public override IAstNode VisitIfStmt(TypedGMLParser.IfStmtContext context) =>
        new IfStatementNode(Node(context.expression(0)), Node(context.block(0)), Enumerable.Range(1, context.expression().Length - 1).Take(context.block().Length - 1).Select(i => new ElseIfClauseNode(Node(context.expression(i)), Node(context.block(i)), Location(context.block(i)))).ToList(), context.ELSE().Length > context.expression().Length ? Node(context.block(context.block().Length - 1)) : null, Location(context));

    public override IAstNode VisitWhileStmt(TypedGMLParser.WhileStmtContext context) =>
        new WhileStatementNode(Node(context.expression()), Node(context.block()), Location(context));

    public override IAstNode VisitForStmt(TypedGMLParser.ForStmtContext context) =>
        new ForStatementNode(Node(context.forInit()), Node(context.expression()), context.forUpdate() is null ? [] : Nodes<IAstNode>(context.forUpdate().expression()), Node(context.block()), Location(context));

    public override IAstNode VisitForInit(TypedGMLParser.ForInitContext context)
    {
        if (context.VAR() is not null)
            return new VarDeclarationStatementNode(Text(context.nameId()), null, Node(context.expression()), true, Location(context));
        if (context.typeRef() is not null)
            return new VarDeclarationStatementNode(Text(context.nameId()), Text(context.typeRef()), Node(context.expression()), false, Location(context));
        if (context.expression() is not null)
            return Node(context.expression());
        return null!;
    }

    public override IAstNode VisitRepeatStmt(TypedGMLParser.RepeatStmtContext context) =>
        new RepeatStatementNode(Node(context.expression()), Node(context.block()), Location(context));

    public override IAstNode VisitSwitchStmt(TypedGMLParser.SwitchStmtContext context) =>
        new SwitchStatementNode(Node(context.expression()), Nodes<SwitchSectionNode>(context.switchSection()), Location(context));

    public override IAstNode VisitSwitchSection(TypedGMLParser.SwitchSectionContext context) =>
        new SwitchSectionNode(context.DEFAULT() is null ? Node(context.expression()) : null, Nodes<IAstNode>(context.statement()), Location(context));

    public override IAstNode VisitWithStmt(TypedGMLParser.WithStmtContext context) =>
        new WithStatementNode(Node(context.expression()), Node(context.block()), Location(context));

    public override IAstNode VisitReturnStmt(TypedGMLParser.ReturnStmtContext context) =>
        new ReturnStatementNode(Node(context.expression()), Location(context));

    public override IAstNode VisitBreakStmt(TypedGMLParser.BreakStmtContext context) => new BreakStatementNode(Location(context));
    public override IAstNode VisitContinueStmt(TypedGMLParser.ContinueStmtContext context) => new ContinueStatementNode(Location(context));

    public override IAstNode VisitTryStmt(TypedGMLParser.TryStmtContext context) =>
        new TryStatementNode(Node(context.block()), Nodes<CatchClauseNode>(context.catchClause()), Node(context.finallyClause()), Location(context));

    public override IAstNode VisitThrowStmt(TypedGMLParser.ThrowStmtContext context) =>
        new ThrowStatementNode(Node(context.expression()), Location(context));

    public override IAstNode VisitCatchClause(TypedGMLParser.CatchClauseContext context) =>
        new CatchClauseNode(Text(context.typeRef()), Text(context.nameId()), Node(context.block()), Location(context));

    public override IAstNode VisitFinallyClause(TypedGMLParser.FinallyClauseContext context) => Node(context.block());

    public override IAstNode VisitRawStmt(TypedGMLParser.RawStmtContext context) =>
        new RawStatementNode(context.RAW_LINE().GetText(), Location(context));
}
