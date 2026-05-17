// Generated from c:/Users/xoggas/Documents/GitHub/typed-gml/TypedGML.Compiler/Grammar/TypedGML.g4 by ANTLR 4.13.1
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
	 * Enter a parse tree produced by {@link TypedGMLParser#typeDecl}.
	 * @param ctx the parse tree
	 */
	void enterTypeDecl(TypedGMLParser.TypeDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#typeDecl}.
	 * @param ctx the parse tree
	 */
	void exitTypeDecl(TypedGMLParser.TypeDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#functionDecl}.
	 * @param ctx the parse tree
	 */
	void enterFunctionDecl(TypedGMLParser.FunctionDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#functionDecl}.
	 * @param ctx the parse tree
	 */
	void exitFunctionDecl(TypedGMLParser.FunctionDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#topLevelDecl}.
	 * @param ctx the parse tree
	 */
	void enterTopLevelDecl(TypedGMLParser.TopLevelDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#topLevelDecl}.
	 * @param ctx the parse tree
	 */
	void exitTopLevelDecl(TypedGMLParser.TopLevelDeclContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#structDecl}.
	 * @param ctx the parse tree
	 */
	void enterStructDecl(TypedGMLParser.StructDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#structDecl}.
	 * @param ctx the parse tree
	 */
	void exitStructDecl(TypedGMLParser.StructDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#enumDecl}.
	 * @param ctx the parse tree
	 */
	void enterEnumDecl(TypedGMLParser.EnumDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#enumDecl}.
	 * @param ctx the parse tree
	 */
	void exitEnumDecl(TypedGMLParser.EnumDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#enumMember}.
	 * @param ctx the parse tree
	 */
	void enterEnumMember(TypedGMLParser.EnumMemberContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#enumMember}.
	 * @param ctx the parse tree
	 */
	void exitEnumMember(TypedGMLParser.EnumMemberContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceDecl(TypedGMLParser.InterfaceDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceDecl(TypedGMLParser.InterfaceDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#delegateDecl}.
	 * @param ctx the parse tree
	 */
	void enterDelegateDecl(TypedGMLParser.DelegateDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#delegateDecl}.
	 * @param ctx the parse tree
	 */
	void exitDelegateDecl(TypedGMLParser.DelegateDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#typeParams}.
	 * @param ctx the parse tree
	 */
	void enterTypeParams(TypedGMLParser.TypeParamsContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#typeParams}.
	 * @param ctx the parse tree
	 */
	void exitTypeParams(TypedGMLParser.TypeParamsContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#typeParam}.
	 * @param ctx the parse tree
	 */
	void enterTypeParam(TypedGMLParser.TypeParamContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#typeParam}.
	 * @param ctx the parse tree
	 */
	void exitTypeParam(TypedGMLParser.TypeParamContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#typeArgs}.
	 * @param ctx the parse tree
	 */
	void enterTypeArgs(TypedGMLParser.TypeArgsContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#typeArgs}.
	 * @param ctx the parse tree
	 */
	void exitTypeArgs(TypedGMLParser.TypeArgsContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#inheritanceList}.
	 * @param ctx the parse tree
	 */
	void enterInheritanceList(TypedGMLParser.InheritanceListContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#inheritanceList}.
	 * @param ctx the parse tree
	 */
	void exitInheritanceList(TypedGMLParser.InheritanceListContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#typeRef}.
	 * @param ctx the parse tree
	 */
	void enterTypeRef(TypedGMLParser.TypeRefContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#typeRef}.
	 * @param ctx the parse tree
	 */
	void exitTypeRef(TypedGMLParser.TypeRefContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#nameId}.
	 * @param ctx the parse tree
	 */
	void enterNameId(TypedGMLParser.NameIdContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#nameId}.
	 * @param ctx the parse tree
	 */
	void exitNameId(TypedGMLParser.NameIdContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#memberDecl}.
	 * @param ctx the parse tree
	 */
	void enterMemberDecl(TypedGMLParser.MemberDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#memberDecl}.
	 * @param ctx the parse tree
	 */
	void exitMemberDecl(TypedGMLParser.MemberDeclContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#propertyDecl}.
	 * @param ctx the parse tree
	 */
	void enterPropertyDecl(TypedGMLParser.PropertyDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#propertyDecl}.
	 * @param ctx the parse tree
	 */
	void exitPropertyDecl(TypedGMLParser.PropertyDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#indexerDecl}.
	 * @param ctx the parse tree
	 */
	void enterIndexerDecl(TypedGMLParser.IndexerDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#indexerDecl}.
	 * @param ctx the parse tree
	 */
	void exitIndexerDecl(TypedGMLParser.IndexerDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#accessorDecl}.
	 * @param ctx the parse tree
	 */
	void enterAccessorDecl(TypedGMLParser.AccessorDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#accessorDecl}.
	 * @param ctx the parse tree
	 */
	void exitAccessorDecl(TypedGMLParser.AccessorDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#methodDecl}.
	 * @param ctx the parse tree
	 */
	void enterMethodDecl(TypedGMLParser.MethodDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#methodDecl}.
	 * @param ctx the parse tree
	 */
	void exitMethodDecl(TypedGMLParser.MethodDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#overloadableOperator}.
	 * @param ctx the parse tree
	 */
	void enterOverloadableOperator(TypedGMLParser.OverloadableOperatorContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#overloadableOperator}.
	 * @param ctx the parse tree
	 */
	void exitOverloadableOperator(TypedGMLParser.OverloadableOperatorContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#constructorDecl}.
	 * @param ctx the parse tree
	 */
	void enterConstructorDecl(TypedGMLParser.ConstructorDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#constructorDecl}.
	 * @param ctx the parse tree
	 */
	void exitConstructorDecl(TypedGMLParser.ConstructorDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#staticConstructorDecl}.
	 * @param ctx the parse tree
	 */
	void enterStaticConstructorDecl(TypedGMLParser.StaticConstructorDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#staticConstructorDecl}.
	 * @param ctx the parse tree
	 */
	void exitStaticConstructorDecl(TypedGMLParser.StaticConstructorDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#eventDecl}.
	 * @param ctx the parse tree
	 */
	void enterEventDecl(TypedGMLParser.EventDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#eventDecl}.
	 * @param ctx the parse tree
	 */
	void exitEventDecl(TypedGMLParser.EventDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceMemberDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceMemberDecl(TypedGMLParser.InterfaceMemberDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceMemberDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceMemberDecl(TypedGMLParser.InterfaceMemberDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceMethodDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceMethodDecl(TypedGMLParser.InterfaceMethodDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceMethodDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceMethodDecl(TypedGMLParser.InterfaceMethodDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfacePropertyDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfacePropertyDecl(TypedGMLParser.InterfacePropertyDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfacePropertyDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfacePropertyDecl(TypedGMLParser.InterfacePropertyDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceIndexerDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceIndexerDecl(TypedGMLParser.InterfaceIndexerDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceIndexerDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceIndexerDecl(TypedGMLParser.InterfaceIndexerDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceEventDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceEventDecl(TypedGMLParser.InterfaceEventDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceEventDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceEventDecl(TypedGMLParser.InterfaceEventDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#interfaceAccessorDecl}.
	 * @param ctx the parse tree
	 */
	void enterInterfaceAccessorDecl(TypedGMLParser.InterfaceAccessorDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#interfaceAccessorDecl}.
	 * @param ctx the parse tree
	 */
	void exitInterfaceAccessorDecl(TypedGMLParser.InterfaceAccessorDeclContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#classMod}.
	 * @param ctx the parse tree
	 */
	void enterClassMod(TypedGMLParser.ClassModContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#classMod}.
	 * @param ctx the parse tree
	 */
	void exitClassMod(TypedGMLParser.ClassModContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#propertyModifiers}.
	 * @param ctx the parse tree
	 */
	void enterPropertyModifiers(TypedGMLParser.PropertyModifiersContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#propertyModifiers}.
	 * @param ctx the parse tree
	 */
	void exitPropertyModifiers(TypedGMLParser.PropertyModifiersContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#methodModifiers}.
	 * @param ctx the parse tree
	 */
	void enterMethodModifiers(TypedGMLParser.MethodModifiersContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#methodModifiers}.
	 * @param ctx the parse tree
	 */
	void exitMethodModifiers(TypedGMLParser.MethodModifiersContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#paramList}.
	 * @param ctx the parse tree
	 */
	void enterParamList(TypedGMLParser.ParamListContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#paramList}.
	 * @param ctx the parse tree
	 */
	void exitParamList(TypedGMLParser.ParamListContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#param}.
	 * @param ctx the parse tree
	 */
	void enterParam(TypedGMLParser.ParamContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#param}.
	 * @param ctx the parse tree
	 */
	void exitParam(TypedGMLParser.ParamContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#argList}.
	 * @param ctx the parse tree
	 */
	void enterArgList(TypedGMLParser.ArgListContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#argList}.
	 * @param ctx the parse tree
	 */
	void exitArgList(TypedGMLParser.ArgListContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#arg}.
	 * @param ctx the parse tree
	 */
	void enterArg(TypedGMLParser.ArgContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#arg}.
	 * @param ctx the parse tree
	 */
	void exitArg(TypedGMLParser.ArgContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#block}.
	 * @param ctx the parse tree
	 */
	void enterBlock(TypedGMLParser.BlockContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#block}.
	 * @param ctx the parse tree
	 */
	void exitBlock(TypedGMLParser.BlockContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#statement}.
	 * @param ctx the parse tree
	 */
	void enterStatement(TypedGMLParser.StatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#statement}.
	 * @param ctx the parse tree
	 */
	void exitStatement(TypedGMLParser.StatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#localVarDecl}.
	 * @param ctx the parse tree
	 */
	void enterLocalVarDecl(TypedGMLParser.LocalVarDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#localVarDecl}.
	 * @param ctx the parse tree
	 */
	void exitLocalVarDecl(TypedGMLParser.LocalVarDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#expressionStmt}.
	 * @param ctx the parse tree
	 */
	void enterExpressionStmt(TypedGMLParser.ExpressionStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#expressionStmt}.
	 * @param ctx the parse tree
	 */
	void exitExpressionStmt(TypedGMLParser.ExpressionStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#ifStmt}.
	 * @param ctx the parse tree
	 */
	void enterIfStmt(TypedGMLParser.IfStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#ifStmt}.
	 * @param ctx the parse tree
	 */
	void exitIfStmt(TypedGMLParser.IfStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#whileStmt}.
	 * @param ctx the parse tree
	 */
	void enterWhileStmt(TypedGMLParser.WhileStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#whileStmt}.
	 * @param ctx the parse tree
	 */
	void exitWhileStmt(TypedGMLParser.WhileStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#forStmt}.
	 * @param ctx the parse tree
	 */
	void enterForStmt(TypedGMLParser.ForStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#forStmt}.
	 * @param ctx the parse tree
	 */
	void exitForStmt(TypedGMLParser.ForStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#forInit}.
	 * @param ctx the parse tree
	 */
	void enterForInit(TypedGMLParser.ForInitContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#forInit}.
	 * @param ctx the parse tree
	 */
	void exitForInit(TypedGMLParser.ForInitContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#forUpdate}.
	 * @param ctx the parse tree
	 */
	void enterForUpdate(TypedGMLParser.ForUpdateContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#forUpdate}.
	 * @param ctx the parse tree
	 */
	void exitForUpdate(TypedGMLParser.ForUpdateContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#repeatStmt}.
	 * @param ctx the parse tree
	 */
	void enterRepeatStmt(TypedGMLParser.RepeatStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#repeatStmt}.
	 * @param ctx the parse tree
	 */
	void exitRepeatStmt(TypedGMLParser.RepeatStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#switchStmt}.
	 * @param ctx the parse tree
	 */
	void enterSwitchStmt(TypedGMLParser.SwitchStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#switchStmt}.
	 * @param ctx the parse tree
	 */
	void exitSwitchStmt(TypedGMLParser.SwitchStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#switchSection}.
	 * @param ctx the parse tree
	 */
	void enterSwitchSection(TypedGMLParser.SwitchSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#switchSection}.
	 * @param ctx the parse tree
	 */
	void exitSwitchSection(TypedGMLParser.SwitchSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#withStmt}.
	 * @param ctx the parse tree
	 */
	void enterWithStmt(TypedGMLParser.WithStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#withStmt}.
	 * @param ctx the parse tree
	 */
	void exitWithStmt(TypedGMLParser.WithStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#returnStmt}.
	 * @param ctx the parse tree
	 */
	void enterReturnStmt(TypedGMLParser.ReturnStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#returnStmt}.
	 * @param ctx the parse tree
	 */
	void exitReturnStmt(TypedGMLParser.ReturnStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#breakStmt}.
	 * @param ctx the parse tree
	 */
	void enterBreakStmt(TypedGMLParser.BreakStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#breakStmt}.
	 * @param ctx the parse tree
	 */
	void exitBreakStmt(TypedGMLParser.BreakStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#continueStmt}.
	 * @param ctx the parse tree
	 */
	void enterContinueStmt(TypedGMLParser.ContinueStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#continueStmt}.
	 * @param ctx the parse tree
	 */
	void exitContinueStmt(TypedGMLParser.ContinueStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#tryStmt}.
	 * @param ctx the parse tree
	 */
	void enterTryStmt(TypedGMLParser.TryStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#tryStmt}.
	 * @param ctx the parse tree
	 */
	void exitTryStmt(TypedGMLParser.TryStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#throwStmt}.
	 * @param ctx the parse tree
	 */
	void enterThrowStmt(TypedGMLParser.ThrowStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#throwStmt}.
	 * @param ctx the parse tree
	 */
	void exitThrowStmt(TypedGMLParser.ThrowStmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#catchClause}.
	 * @param ctx the parse tree
	 */
	void enterCatchClause(TypedGMLParser.CatchClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#catchClause}.
	 * @param ctx the parse tree
	 */
	void exitCatchClause(TypedGMLParser.CatchClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#finallyClause}.
	 * @param ctx the parse tree
	 */
	void enterFinallyClause(TypedGMLParser.FinallyClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#finallyClause}.
	 * @param ctx the parse tree
	 */
	void exitFinallyClause(TypedGMLParser.FinallyClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#rawStmt}.
	 * @param ctx the parse tree
	 */
	void enterRawStmt(TypedGMLParser.RawStmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#rawStmt}.
	 * @param ctx the parse tree
	 */
	void exitRawStmt(TypedGMLParser.RawStmtContext ctx);
	/**
	 * Enter a parse tree produced by the {@code newObjectExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNewObjectExpr(TypedGMLParser.NewObjectExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code newObjectExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNewObjectExpr(TypedGMLParser.NewObjectExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code thisExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterThisExpr(TypedGMLParser.ThisExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code thisExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitThisExpr(TypedGMLParser.ThisExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code nullExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNullExpr(TypedGMLParser.NullExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code nullExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNullExpr(TypedGMLParser.NullExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code castExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterCastExpr(TypedGMLParser.CastExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code castExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitCastExpr(TypedGMLParser.CastExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code fieldAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterFieldAccessExpr(TypedGMLParser.FieldAccessExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code fieldAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitFieldAccessExpr(TypedGMLParser.FieldAccessExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code arrayInitExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterArrayInitExpr(TypedGMLParser.ArrayInitExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code arrayInitExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitArrayInitExpr(TypedGMLParser.ArrayInitExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code typeofExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterTypeofExpr(TypedGMLParser.TypeofExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code typeofExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitTypeofExpr(TypedGMLParser.TypeofExprContext ctx);
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
	 * Enter a parse tree produced by the {@code funcCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterFuncCallExpr(TypedGMLParser.FuncCallExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code funcCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitFuncCallExpr(TypedGMLParser.FuncCallExprContext ctx);
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
	 * Enter a parse tree produced by the {@code rightShiftExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterRightShiftExpr(TypedGMLParser.RightShiftExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code rightShiftExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitRightShiftExpr(TypedGMLParser.RightShiftExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code defaultOfExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterDefaultOfExpr(TypedGMLParser.DefaultOfExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code defaultOfExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitDefaultOfExpr(TypedGMLParser.DefaultOfExprContext ctx);
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
	 * Enter a parse tree produced by the {@code methodCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterMethodCallExpr(TypedGMLParser.MethodCallExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code methodCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitMethodCallExpr(TypedGMLParser.MethodCallExprContext ctx);
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
	 * Enter a parse tree produced by the {@code baseCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBaseCallExpr(TypedGMLParser.BaseCallExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code baseCallExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBaseCallExpr(TypedGMLParser.BaseCallExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code ternaryExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterTernaryExpr(TypedGMLParser.TernaryExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code ternaryExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitTernaryExpr(TypedGMLParser.TernaryExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code dictInitExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterDictInitExpr(TypedGMLParser.DictInitExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code dictInitExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitDictInitExpr(TypedGMLParser.DictInitExprContext ctx);
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
	 * Enter a parse tree produced by the {@code assignExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterAssignExpr(TypedGMLParser.AssignExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code assignExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitAssignExpr(TypedGMLParser.AssignExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code isExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterIsExpr(TypedGMLParser.IsExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code isExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitIsExpr(TypedGMLParser.IsExprContext ctx);
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
	 * Enter a parse tree produced by the {@code fieldKeywordExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterFieldKeywordExpr(TypedGMLParser.FieldKeywordExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code fieldKeywordExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitFieldKeywordExpr(TypedGMLParser.FieldKeywordExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code invokeExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterInvokeExpr(TypedGMLParser.InvokeExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code invokeExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitInvokeExpr(TypedGMLParser.InvokeExprContext ctx);
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
	 * Enter a parse tree produced by the {@code baseAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterBaseAccessExpr(TypedGMLParser.BaseAccessExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code baseAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitBaseAccessExpr(TypedGMLParser.BaseAccessExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code valueKeywordExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterValueKeywordExpr(TypedGMLParser.ValueKeywordExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code valueKeywordExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitValueKeywordExpr(TypedGMLParser.ValueKeywordExprContext ctx);
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
	 * Enter a parse tree produced by the {@code nullCoalesceExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNullCoalesceExpr(TypedGMLParser.NullCoalesceExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code nullCoalesceExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNullCoalesceExpr(TypedGMLParser.NullCoalesceExprContext ctx);
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
	 * Enter a parse tree produced by the {@code lambdaExprAtom}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterLambdaExprAtom(TypedGMLParser.LambdaExprAtomContext ctx);
	/**
	 * Exit a parse tree produced by the {@code lambdaExprAtom}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitLambdaExprAtom(TypedGMLParser.LambdaExprAtomContext ctx);
	/**
	 * Enter a parse tree produced by the {@code nullConditionalAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNullConditionalAccessExpr(TypedGMLParser.NullConditionalAccessExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code nullConditionalAccessExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNullConditionalAccessExpr(TypedGMLParser.NullConditionalAccessExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code nameofExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterNameofExpr(TypedGMLParser.NameofExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code nameofExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitNameofExpr(TypedGMLParser.NameofExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code indexExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterIndexExpr(TypedGMLParser.IndexExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code indexExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitIndexExpr(TypedGMLParser.IndexExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code defaultExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterDefaultExpr(TypedGMLParser.DefaultExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code defaultExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitDefaultExpr(TypedGMLParser.DefaultExprContext ctx);
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
	 * Enter a parse tree produced by the {@code asExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterAsExpr(TypedGMLParser.AsExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code asExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitAsExpr(TypedGMLParser.AsExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code leftShiftExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterLeftShiftExpr(TypedGMLParser.LeftShiftExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code leftShiftExpr}
	 * labeled alternative in {@link TypedGMLParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitLeftShiftExpr(TypedGMLParser.LeftShiftExprContext ctx);
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
	 * Enter a parse tree produced by {@link TypedGMLParser#lambdaExpr}.
	 * @param ctx the parse tree
	 */
	void enterLambdaExpr(TypedGMLParser.LambdaExprContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#lambdaExpr}.
	 * @param ctx the parse tree
	 */
	void exitLambdaExpr(TypedGMLParser.LambdaExprContext ctx);
	/**
	 * Enter a parse tree produced by {@link TypedGMLParser#dictionaryEntry}.
	 * @param ctx the parse tree
	 */
	void enterDictionaryEntry(TypedGMLParser.DictionaryEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link TypedGMLParser#dictionaryEntry}.
	 * @param ctx the parse tree
	 */
	void exitDictionaryEntry(TypedGMLParser.DictionaryEntryContext ctx);
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