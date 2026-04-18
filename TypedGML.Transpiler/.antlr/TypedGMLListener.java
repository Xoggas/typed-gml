// Generated from c:/Users/xoggas/Documents/GitHub/typed-gml/TypedGML.Transpiler/TypedGML.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link TypedGMLParser}.
 */
public interface TypedGMLListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#program}.
	 * @param ctx the parse tree
	 */
	void enterProgram(TypedGMLParser.ProgramContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#program}.
	 * @param ctx the parse tree
	 */
	void exitProgram(TypedGMLParser.ProgramContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#qualifiedName}.
	 * @param ctx the parse tree
	 */
	void enterQualifiedName(TypedGMLParser.QualifiedNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#qualifiedName}.
	 * @param ctx the parse tree
	 */
	void exitQualifiedName(TypedGMLParser.QualifiedNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#usingDecl}.
	 * @param ctx the parse tree
	 */
	void enterUsingDecl(TypedGMLParser.UsingDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#usingDecl}.
	 * @param ctx the parse tree
	 */
	void exitUsingDecl(TypedGMLParser.UsingDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#namespaceDecl}.
	 * @param ctx the parse tree
	 */
	void enterNamespaceDecl(TypedGMLParser.NamespaceDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#namespaceDecl}.
	 * @param ctx the parse tree
	 */
	void exitNamespaceDecl(TypedGMLParser.NamespaceDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#classDecl}.
	 * @param ctx the parse tree
	 */
	void enterClassDecl(TypedGMLParser.ClassDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#classDecl}.
	 * @param ctx the parse tree
	 */
	void exitClassDecl(TypedGMLParser.ClassDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#fieldDecl}.
	 * @param ctx the parse tree
	 */
	void enterFieldDecl(TypedGMLParser.FieldDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#fieldDecl}.
	 * @param ctx the parse tree
	 */
	void exitFieldDecl(TypedGMLParser.FieldDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#decorator}.
	 * @param ctx the parse tree
	 */
	void enterDecorator(TypedGMLParser.DecoratorContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#decorator}.
	 * @param ctx the parse tree
	 */
	void exitDecorator(TypedGMLParser.DecoratorContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#accessMod}.
	 * @param ctx the parse tree
	 */
	void enterAccessMod(TypedGMLParser.AccessModContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#accessMod}.
	 * @param ctx the parse tree
	 */
	void exitAccessMod(TypedGMLParser.AccessModContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#fieldModifiers}.
	 * @param ctx the parse tree
	 */
	void enterFieldModifiers(TypedGMLParser.FieldModifiersContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#fieldModifiers}.
	 * @param ctx the parse tree
	 */
	void exitFieldModifiers(TypedGMLParser.FieldModifiersContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#scopeMod}.
	 * @param ctx the parse tree
	 */
	void enterScopeMod(TypedGMLParser.ScopeModContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#scopeMod}.
	 * @param ctx the parse tree
	 */
	void exitScopeMod(TypedGMLParser.ScopeModContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#classInheritanceMod}.
	 * @param ctx the parse tree
	 */
	void enterClassInheritanceMod(TypedGMLParser.ClassInheritanceModContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#classInheritanceMod}.
	 * @param ctx the parse tree
	 */
	void exitClassInheritanceMod(TypedGMLParser.ClassInheritanceModContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#parameterList}.
	 * @param ctx the parse tree
	 */
	void enterParameterList(TypedGMLParser.ParameterListContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#parameterList}.
	 * @param ctx the parse tree
	 */
	void exitParameterList(TypedGMLParser.ParameterListContext ctx);
	/**
	 * Enter a parse tree produced by the {@code newExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNewExpr(TypedGMLParser.NewExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code newExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNewExpr(TypedGMLParser.NewExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code mulDivMod}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterMulDivMod(TypedGMLParser.MulDivModContext ctx);
	/**
	 * Exit a parse tree produced by the {@code mulDivMod}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitMulDivMod(TypedGMLParser.MulDivModContext ctx);
	/**
	 * Enter a parse tree produced by the {@code intExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterIntExpr(TypedGMLParser.IntExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code intExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitIntExpr(TypedGMLParser.IntExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code comparison}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterComparison(TypedGMLParser.ComparisonContext ctx);
	/**
	 * Exit a parse tree produced by the {@code comparison}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitComparison(TypedGMLParser.ComparisonContext ctx);
	/**
	 * Enter a parse tree produced by the {@code realExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterRealExpr(TypedGMLParser.RealExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code realExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitRealExpr(TypedGMLParser.RealExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code shift}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterShift(TypedGMLParser.ShiftContext ctx);
	/**
	 * Exit a parse tree produced by the {@code shift}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitShift(TypedGMLParser.ShiftContext ctx);
	/**
	 * Enter a parse tree produced by the {@code bitwiseXor}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBitwiseXor(TypedGMLParser.BitwiseXorContext ctx);
	/**
	 * Exit a parse tree produced by the {@code bitwiseXor}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBitwiseXor(TypedGMLParser.BitwiseXorContext ctx);
	/**
	 * Enter a parse tree produced by the {@code fieldAccess}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterFieldAccess(TypedGMLParser.FieldAccessContext ctx);
	/**
	 * Exit a parse tree produced by the {@code fieldAccess}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitFieldAccess(TypedGMLParser.FieldAccessContext ctx);
	/**
	 * Enter a parse tree produced by the {@code logicalAnd}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterLogicalAnd(TypedGMLParser.LogicalAndContext ctx);
	/**
	 * Exit a parse tree produced by the {@code logicalAnd}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitLogicalAnd(TypedGMLParser.LogicalAndContext ctx);
	/**
	 * Enter a parse tree produced by the {@code addSub}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterAddSub(TypedGMLParser.AddSubContext ctx);
	/**
	 * Exit a parse tree produced by the {@code addSub}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitAddSub(TypedGMLParser.AddSubContext ctx);
	/**
	 * Enter a parse tree produced by the {@code bitwiseAnd}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBitwiseAnd(TypedGMLParser.BitwiseAndContext ctx);
	/**
	 * Exit a parse tree produced by the {@code bitwiseAnd}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBitwiseAnd(TypedGMLParser.BitwiseAndContext ctx);
	/**
	 * Enter a parse tree produced by the {@code parenExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterParenExpr(TypedGMLParser.ParenExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code parenExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitParenExpr(TypedGMLParser.ParenExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code stringExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterStringExpr(TypedGMLParser.StringExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code stringExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitStringExpr(TypedGMLParser.StringExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code unaryExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterUnaryExpr(TypedGMLParser.UnaryExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code unaryExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitUnaryExpr(TypedGMLParser.UnaryExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code bitwiseOr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBitwiseOr(TypedGMLParser.BitwiseOrContext ctx);
	/**
	 * Exit a parse tree produced by the {@code bitwiseOr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBitwiseOr(TypedGMLParser.BitwiseOrContext ctx);
	/**
	 * Enter a parse tree produced by the {@code funcCall}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterFuncCall(TypedGMLParser.FuncCallContext ctx);
	/**
	 * Exit a parse tree produced by the {@code funcCall}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitFuncCall(TypedGMLParser.FuncCallContext ctx);
	/**
	 * Enter a parse tree produced by the {@code boolExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBoolExpr(TypedGMLParser.BoolExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code boolExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBoolExpr(TypedGMLParser.BoolExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code logicalOr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterLogicalOr(TypedGMLParser.LogicalOrContext ctx);
	/**
	 * Exit a parse tree produced by the {@code logicalOr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitLogicalOr(TypedGMLParser.LogicalOrContext ctx);
	/**
	 * Enter a parse tree produced by the {@code idExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterIdExpr(TypedGMLParser.IdExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code idExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitIdExpr(TypedGMLParser.IdExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code methodCall}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterMethodCall(TypedGMLParser.MethodCallContext ctx);
	/**
	 * Exit a parse tree produced by the {@code methodCall}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitMethodCall(TypedGMLParser.MethodCallContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#intLiteral}.
	 * @param ctx the parse tree
	 */
	void enterIntLiteral(TypedGMLParser.IntLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#intLiteral}.
	 * @param ctx the parse tree
	 */
	void exitIntLiteral(TypedGMLParser.IntLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#realLiteral}.
	 * @param ctx the parse tree
	 */
	void enterRealLiteral(TypedGMLParser.RealLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#realLiteral}.
	 * @param ctx the parse tree
	 */
	void exitRealLiteral(TypedGMLParser.RealLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#stringLiteral}.
	 * @param ctx the parse tree
	 */
	void enterStringLiteral(TypedGMLParser.StringLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#stringLiteral}.
	 * @param ctx the parse tree
	 */
	void exitStringLiteral(TypedGMLParser.StringLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#boolLiteral}.
	 * @param ctx the parse tree
	 */
	void enterBoolLiteral(TypedGMLParser.BoolLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#boolLiteral}.
	 * @param ctx the parse tree
	 */
	void exitBoolLiteral(TypedGMLParser.BoolLiteralContext ctx);
}