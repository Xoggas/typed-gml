// Generated from c:/Users/xoggas/Documents/GitHub/typed-gml/TypedGML.Transpiler/TypedGML.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast", "CheckReturnValue"})
public class TypedGMLParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.13.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, CLASS=3, STRUCT=4, ENUM=5, TRUE=6, FALSE=7, PUBLIC=8, 
		PROTECTED=9, PRIVATE=10, READONLY=11, STATIC=12, CONST=13, NEW=14, ABSTRACT=15, 
		SEALED=16, OVERRIDE=17, VIRTUAL=18, AND=19, OR=20, NOT=21, ASSIGN=22, 
		EQ=23, NEQ=24, LT=25, GT=26, LE=27, GE=28, PLUS=29, MINUS=30, STAR=31, 
		SLASH=32, PERCENT=33, BITAND=34, BITOR=35, BITXOR=36, BITNOT=37, LSHIFT=38, 
		RSHIFT=39, LPAREN=40, RPAREN=41, COMMA=42, PERIOD=43, LBRACE=44, RBRACE=45, 
		SEMI=46, COLON=47, AT=48, STRING_LITERAL=49, REAL=50, INTEGER=51, ID=52, 
		WS=53, LINE_COMMENT=54, BLOCK_COMMENT=55;
	public static final int
		RULE_program = 0, RULE_qualifiedName = 1, RULE_usingDecl = 2, RULE_namespaceDecl = 3, 
		RULE_classDecl = 4, RULE_fieldDecl = 5, RULE_decorator = 6, RULE_accessMod = 7, 
		RULE_fieldModifiers = 8, RULE_scopeMod = 9, RULE_classInheritanceMod = 10, 
		RULE_parameterList = 11, RULE_expression = 12, RULE_intLiteral = 13, RULE_realLiteral = 14, 
		RULE_stringLiteral = 15, RULE_boolLiteral = 16;
	private static String[] makeRuleNames() {
		return new String[] {
			"program", "qualifiedName", "usingDecl", "namespaceDecl", "classDecl", 
			"fieldDecl", "decorator", "accessMod", "fieldModifiers", "scopeMod", 
			"classInheritanceMod", "parameterList", "expression", "intLiteral", "realLiteral", 
			"stringLiteral", "boolLiteral"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'using'", "'namespace'", "'class'", "'struct'", "'enum'", "'true'", 
			"'false'", "'public'", "'protected'", "'private'", "'readonly'", "'static'", 
			"'const'", "'new'", "'abstract'", "'sealed'", "'override'", "'virtual'", 
			"'and'", "'or'", "'not'", "'='", "'=='", "'!='", "'<'", "'>'", "'<='", 
			"'>='", "'+'", "'-'", "'*'", "'/'", "'%'", "'&'", "'|'", "'^'", "'~'", 
			"'<<'", "'>>'", "'('", "')'", "','", "'.'", "'{'", "'}'", "';'", "':'", 
			"'@'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, null, null, "CLASS", "STRUCT", "ENUM", "TRUE", "FALSE", "PUBLIC", 
			"PROTECTED", "PRIVATE", "READONLY", "STATIC", "CONST", "NEW", "ABSTRACT", 
			"SEALED", "OVERRIDE", "VIRTUAL", "AND", "OR", "NOT", "ASSIGN", "EQ", 
			"NEQ", "LT", "GT", "LE", "GE", "PLUS", "MINUS", "STAR", "SLASH", "PERCENT", 
			"BITAND", "BITOR", "BITXOR", "BITNOT", "LSHIFT", "RSHIFT", "LPAREN", 
			"RPAREN", "COMMA", "PERIOD", "LBRACE", "RBRACE", "SEMI", "COLON", "AT", 
			"STRING_LITERAL", "REAL", "INTEGER", "ID", "WS", "LINE_COMMENT", "BLOCK_COMMENT"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "TypedGML.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public TypedGMLParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ProgramContext extends ParserRuleContext {
		public List<UsingDeclContext> usingDecl() {
			return getRuleContexts(UsingDeclContext.class);
		}
		public UsingDeclContext usingDecl(int i) {
			return getRuleContext(UsingDeclContext.class,i);
		}
		public List<NamespaceDeclContext> namespaceDecl() {
			return getRuleContexts(NamespaceDeclContext.class);
		}
		public NamespaceDeclContext namespaceDecl(int i) {
			return getRuleContext(NamespaceDeclContext.class,i);
		}
		public List<ClassDeclContext> classDecl() {
			return getRuleContexts(ClassDeclContext.class);
		}
		public ClassDeclContext classDecl(int i) {
			return getRuleContext(ClassDeclContext.class,i);
		}
		public ProgramContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_program; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterProgram(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitProgram(this);
		}
	}

	public final ProgramContext program() throws RecognitionException {
		ProgramContext _localctx = new ProgramContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_program);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(37);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__0) {
				{
				{
				setState(34);
				usingDecl();
				}
				}
				setState(39);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(41); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(40);
				namespaceDecl();
				}
				}
				setState(43); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( _la==T__1 );
			setState(48);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 281474976712448L) != 0)) {
				{
				{
				setState(45);
				classDecl();
				}
				}
				setState(50);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class QualifiedNameContext extends ParserRuleContext {
		public List<TerminalNode> ID() { return getTokens(TypedGMLParser.ID); }
		public TerminalNode ID(int i) {
			return getToken(TypedGMLParser.ID, i);
		}
		public List<TerminalNode> PERIOD() { return getTokens(TypedGMLParser.PERIOD); }
		public TerminalNode PERIOD(int i) {
			return getToken(TypedGMLParser.PERIOD, i);
		}
		public QualifiedNameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_qualifiedName; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterQualifiedName(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitQualifiedName(this);
		}
	}

	public final QualifiedNameContext qualifiedName() throws RecognitionException {
		QualifiedNameContext _localctx = new QualifiedNameContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_qualifiedName);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(51);
			match(ID);
			setState(56);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==PERIOD) {
				{
				{
				setState(52);
				match(PERIOD);
				setState(53);
				match(ID);
				}
				}
				setState(58);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class UsingDeclContext extends ParserRuleContext {
		public QualifiedNameContext qualifiedName() {
			return getRuleContext(QualifiedNameContext.class,0);
		}
		public TerminalNode SEMI() { return getToken(TypedGMLParser.SEMI, 0); }
		public UsingDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_usingDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterUsingDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitUsingDecl(this);
		}
	}

	public final UsingDeclContext usingDecl() throws RecognitionException {
		UsingDeclContext _localctx = new UsingDeclContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_usingDecl);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(59);
			match(T__0);
			setState(60);
			qualifiedName();
			setState(61);
			match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class NamespaceDeclContext extends ParserRuleContext {
		public QualifiedNameContext qualifiedName() {
			return getRuleContext(QualifiedNameContext.class,0);
		}
		public TerminalNode SEMI() { return getToken(TypedGMLParser.SEMI, 0); }
		public NamespaceDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_namespaceDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterNamespaceDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitNamespaceDecl(this);
		}
	}

	public final NamespaceDeclContext namespaceDecl() throws RecognitionException {
		NamespaceDeclContext _localctx = new NamespaceDeclContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_namespaceDecl);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(63);
			match(T__1);
			setState(64);
			qualifiedName();
			setState(65);
			match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ClassDeclContext extends ParserRuleContext {
		public AccessModContext accessMod() {
			return getRuleContext(AccessModContext.class,0);
		}
		public TerminalNode CLASS() { return getToken(TypedGMLParser.CLASS, 0); }
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public TerminalNode LBRACE() { return getToken(TypedGMLParser.LBRACE, 0); }
		public TerminalNode RBRACE() { return getToken(TypedGMLParser.RBRACE, 0); }
		public List<DecoratorContext> decorator() {
			return getRuleContexts(DecoratorContext.class);
		}
		public DecoratorContext decorator(int i) {
			return getRuleContext(DecoratorContext.class,i);
		}
		public ScopeModContext scopeMod() {
			return getRuleContext(ScopeModContext.class,0);
		}
		public ClassInheritanceModContext classInheritanceMod() {
			return getRuleContext(ClassInheritanceModContext.class,0);
		}
		public List<FieldDeclContext> fieldDecl() {
			return getRuleContexts(FieldDeclContext.class);
		}
		public FieldDeclContext fieldDecl(int i) {
			return getRuleContext(FieldDeclContext.class,i);
		}
		public ClassDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_classDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterClassDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitClassDecl(this);
		}
	}

	public final ClassDeclContext classDecl() throws RecognitionException {
		ClassDeclContext _localctx = new ClassDeclContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_classDecl);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(70);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==AT) {
				{
				{
				setState(67);
				decorator();
				}
				}
				setState(72);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(73);
			accessMod();
			setState(76);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case STATIC:
				{
				setState(74);
				scopeMod();
				}
				break;
			case ABSTRACT:
			case SEALED:
				{
				setState(75);
				classInheritanceMod();
				}
				break;
			case CLASS:
				break;
			default:
				break;
			}
			setState(78);
			match(CLASS);
			setState(79);
			match(ID);
			setState(80);
			match(LBRACE);
			setState(84);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 281474976712448L) != 0)) {
				{
				{
				setState(81);
				fieldDecl();
				}
				}
				setState(86);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(87);
			match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class FieldDeclContext extends ParserRuleContext {
		public FieldModifiersContext fieldModifiers() {
			return getRuleContext(FieldModifiersContext.class,0);
		}
		public QualifiedNameContext qualifiedName() {
			return getRuleContext(QualifiedNameContext.class,0);
		}
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public TerminalNode SEMI() { return getToken(TypedGMLParser.SEMI, 0); }
		public List<DecoratorContext> decorator() {
			return getRuleContexts(DecoratorContext.class);
		}
		public DecoratorContext decorator(int i) {
			return getRuleContext(DecoratorContext.class,i);
		}
		public TerminalNode ASSIGN() { return getToken(TypedGMLParser.ASSIGN, 0); }
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public FieldDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_fieldDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterFieldDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitFieldDecl(this);
		}
	}

	public final FieldDeclContext fieldDecl() throws RecognitionException {
		FieldDeclContext _localctx = new FieldDeclContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_fieldDecl);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(92);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==AT) {
				{
				{
				setState(89);
				decorator();
				}
				}
				setState(94);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(95);
			fieldModifiers();
			setState(96);
			qualifiedName();
			setState(97);
			match(ID);
			setState(100);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==ASSIGN) {
				{
				setState(98);
				match(ASSIGN);
				setState(99);
				expression(0);
				}
			}

			setState(102);
			match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class DecoratorContext extends ParserRuleContext {
		public TerminalNode AT() { return getToken(TypedGMLParser.AT, 0); }
		public QualifiedNameContext qualifiedName() {
			return getRuleContext(QualifiedNameContext.class,0);
		}
		public TerminalNode LPAREN() { return getToken(TypedGMLParser.LPAREN, 0); }
		public ParameterListContext parameterList() {
			return getRuleContext(ParameterListContext.class,0);
		}
		public TerminalNode RPAREN() { return getToken(TypedGMLParser.RPAREN, 0); }
		public DecoratorContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_decorator; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterDecorator(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitDecorator(this);
		}
	}

	public final DecoratorContext decorator() throws RecognitionException {
		DecoratorContext _localctx = new DecoratorContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_decorator);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(104);
			match(AT);
			setState(105);
			qualifiedName();
			setState(106);
			match(LPAREN);
			setState(107);
			parameterList();
			setState(108);
			match(RPAREN);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class AccessModContext extends ParserRuleContext {
		public TerminalNode PUBLIC() { return getToken(TypedGMLParser.PUBLIC, 0); }
		public TerminalNode PROTECTED() { return getToken(TypedGMLParser.PROTECTED, 0); }
		public TerminalNode PRIVATE() { return getToken(TypedGMLParser.PRIVATE, 0); }
		public AccessModContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_accessMod; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterAccessMod(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitAccessMod(this);
		}
	}

	public final AccessModContext accessMod() throws RecognitionException {
		AccessModContext _localctx = new AccessModContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_accessMod);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(110);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 1792L) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class FieldModifiersContext extends ParserRuleContext {
		public AccessModContext accessMod() {
			return getRuleContext(AccessModContext.class,0);
		}
		public TerminalNode READONLY() { return getToken(TypedGMLParser.READONLY, 0); }
		public ScopeModContext scopeMod() {
			return getRuleContext(ScopeModContext.class,0);
		}
		public TerminalNode CONST() { return getToken(TypedGMLParser.CONST, 0); }
		public FieldModifiersContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_fieldModifiers; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterFieldModifiers(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitFieldModifiers(this);
		}
	}

	public final FieldModifiersContext fieldModifiers() throws RecognitionException {
		FieldModifiersContext _localctx = new FieldModifiersContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_fieldModifiers);
		int _la;
		try {
			setState(128);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,12,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(112);
				accessMod();
				setState(114);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==STATIC) {
					{
					setState(113);
					scopeMod();
					}
				}

				setState(116);
				match(READONLY);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(118);
				accessMod();
				setState(120);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==STATIC) {
					{
					setState(119);
					scopeMod();
					}
				}

				setState(122);
				match(CONST);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(124);
				accessMod();
				setState(126);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==STATIC) {
					{
					setState(125);
					scopeMod();
					}
				}

				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ScopeModContext extends ParserRuleContext {
		public TerminalNode STATIC() { return getToken(TypedGMLParser.STATIC, 0); }
		public ScopeModContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_scopeMod; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterScopeMod(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitScopeMod(this);
		}
	}

	public final ScopeModContext scopeMod() throws RecognitionException {
		ScopeModContext _localctx = new ScopeModContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_scopeMod);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(130);
			match(STATIC);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ClassInheritanceModContext extends ParserRuleContext {
		public TerminalNode ABSTRACT() { return getToken(TypedGMLParser.ABSTRACT, 0); }
		public TerminalNode SEALED() { return getToken(TypedGMLParser.SEALED, 0); }
		public ClassInheritanceModContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_classInheritanceMod; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterClassInheritanceMod(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitClassInheritanceMod(this);
		}
	}

	public final ClassInheritanceModContext classInheritanceMod() throws RecognitionException {
		ClassInheritanceModContext _localctx = new ClassInheritanceModContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_classInheritanceMod);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(132);
			_la = _input.LA(1);
			if ( !(_la==ABSTRACT || _la==SEALED) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ParameterListContext extends ParserRuleContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(TypedGMLParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(TypedGMLParser.COMMA, i);
		}
		public ParameterListContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_parameterList; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterParameterList(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitParameterList(this);
		}
	}

	public final ParameterListContext parameterList() throws RecognitionException {
		ParameterListContext _localctx = new ParameterListContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_parameterList);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(134);
			expression(0);
			setState(139);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(135);
				match(COMMA);
				setState(136);
				expression(0);
				}
				}
				setState(141);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ExpressionContext extends ParserRuleContext {
		public ExpressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expression; }
	 
		public ExpressionContext() { }
		public void copyFrom(ExpressionContext ctx) {
			super.copyFrom(ctx);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class NewExprContext extends ExpressionContext {
		public TerminalNode NEW() { return getToken(TypedGMLParser.NEW, 0); }
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public TerminalNode LPAREN() { return getToken(TypedGMLParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(TypedGMLParser.RPAREN, 0); }
		public ParameterListContext parameterList() {
			return getRuleContext(ParameterListContext.class,0);
		}
		public NewExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterNewExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitNewExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class MulDivModContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode STAR() { return getToken(TypedGMLParser.STAR, 0); }
		public TerminalNode SLASH() { return getToken(TypedGMLParser.SLASH, 0); }
		public TerminalNode PERCENT() { return getToken(TypedGMLParser.PERCENT, 0); }
		public MulDivModContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterMulDivMod(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitMulDivMod(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class IntExprContext extends ExpressionContext {
		public IntLiteralContext intLiteral() {
			return getRuleContext(IntLiteralContext.class,0);
		}
		public IntExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterIntExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitIntExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class ComparisonContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode EQ() { return getToken(TypedGMLParser.EQ, 0); }
		public TerminalNode NEQ() { return getToken(TypedGMLParser.NEQ, 0); }
		public TerminalNode LT() { return getToken(TypedGMLParser.LT, 0); }
		public TerminalNode GT() { return getToken(TypedGMLParser.GT, 0); }
		public TerminalNode LE() { return getToken(TypedGMLParser.LE, 0); }
		public TerminalNode GE() { return getToken(TypedGMLParser.GE, 0); }
		public ComparisonContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterComparison(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitComparison(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class RealExprContext extends ExpressionContext {
		public RealLiteralContext realLiteral() {
			return getRuleContext(RealLiteralContext.class,0);
		}
		public RealExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterRealExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitRealExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class ShiftContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode LSHIFT() { return getToken(TypedGMLParser.LSHIFT, 0); }
		public TerminalNode RSHIFT() { return getToken(TypedGMLParser.RSHIFT, 0); }
		public ShiftContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterShift(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitShift(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class BitwiseXorContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode BITXOR() { return getToken(TypedGMLParser.BITXOR, 0); }
		public BitwiseXorContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterBitwiseXor(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitBitwiseXor(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class FieldAccessContext extends ExpressionContext {
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode PERIOD() { return getToken(TypedGMLParser.PERIOD, 0); }
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public FieldAccessContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterFieldAccess(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitFieldAccess(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class LogicalAndContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode AND() { return getToken(TypedGMLParser.AND, 0); }
		public LogicalAndContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterLogicalAnd(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitLogicalAnd(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class AddSubContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode PLUS() { return getToken(TypedGMLParser.PLUS, 0); }
		public TerminalNode MINUS() { return getToken(TypedGMLParser.MINUS, 0); }
		public AddSubContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterAddSub(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitAddSub(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class BitwiseAndContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode BITAND() { return getToken(TypedGMLParser.BITAND, 0); }
		public BitwiseAndContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterBitwiseAnd(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitBitwiseAnd(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class ParenExprContext extends ExpressionContext {
		public TerminalNode LPAREN() { return getToken(TypedGMLParser.LPAREN, 0); }
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode RPAREN() { return getToken(TypedGMLParser.RPAREN, 0); }
		public ParenExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterParenExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitParenExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class StringExprContext extends ExpressionContext {
		public StringLiteralContext stringLiteral() {
			return getRuleContext(StringLiteralContext.class,0);
		}
		public StringExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterStringExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitStringExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class UnaryExprContext extends ExpressionContext {
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode MINUS() { return getToken(TypedGMLParser.MINUS, 0); }
		public TerminalNode BITNOT() { return getToken(TypedGMLParser.BITNOT, 0); }
		public TerminalNode NOT() { return getToken(TypedGMLParser.NOT, 0); }
		public UnaryExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterUnaryExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitUnaryExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class BitwiseOrContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode BITOR() { return getToken(TypedGMLParser.BITOR, 0); }
		public BitwiseOrContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterBitwiseOr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitBitwiseOr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class FuncCallContext extends ExpressionContext {
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public TerminalNode LPAREN() { return getToken(TypedGMLParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(TypedGMLParser.RPAREN, 0); }
		public ParameterListContext parameterList() {
			return getRuleContext(ParameterListContext.class,0);
		}
		public FuncCallContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterFuncCall(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitFuncCall(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class BoolExprContext extends ExpressionContext {
		public BoolLiteralContext boolLiteral() {
			return getRuleContext(BoolLiteralContext.class,0);
		}
		public BoolExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterBoolExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitBoolExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class LogicalOrContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode OR() { return getToken(TypedGMLParser.OR, 0); }
		public LogicalOrContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterLogicalOr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitLogicalOr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class IdExprContext extends ExpressionContext {
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public IdExprContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterIdExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitIdExpr(this);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class MethodCallContext extends ExpressionContext {
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode PERIOD() { return getToken(TypedGMLParser.PERIOD, 0); }
		public TerminalNode ID() { return getToken(TypedGMLParser.ID, 0); }
		public TerminalNode LPAREN() { return getToken(TypedGMLParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(TypedGMLParser.RPAREN, 0); }
		public ParameterListContext parameterList() {
			return getRuleContext(ParameterListContext.class,0);
		}
		public MethodCallContext(ExpressionContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterMethodCall(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitMethodCall(this);
		}
	}

	public final ExpressionContext expression() throws RecognitionException {
		return expression(0);
	}

	private ExpressionContext expression(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExpressionContext _localctx = new ExpressionContext(_ctx, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 24;
		enterRecursionRule(_localctx, 24, RULE_expression, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(167);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,16,_ctx) ) {
			case 1:
				{
				_localctx = new UnaryExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;

				setState(143);
				_la = _input.LA(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 138514792448L) != 0)) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(144);
				expression(20);
				}
				break;
			case 2:
				{
				_localctx = new NewExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(145);
				match(NEW);
				setState(146);
				match(ID);
				setState(147);
				match(LPAREN);
				setState(149);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 8445487327756480L) != 0)) {
					{
					setState(148);
					parameterList();
					}
				}

				setState(151);
				match(RPAREN);
				}
				break;
			case 3:
				{
				_localctx = new FuncCallContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(152);
				match(ID);
				setState(153);
				match(LPAREN);
				setState(155);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 8445487327756480L) != 0)) {
					{
					setState(154);
					parameterList();
					}
				}

				setState(157);
				match(RPAREN);
				}
				break;
			case 4:
				{
				_localctx = new ParenExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(158);
				match(LPAREN);
				setState(159);
				expression(0);
				setState(160);
				match(RPAREN);
				}
				break;
			case 5:
				{
				_localctx = new IdExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(162);
				match(ID);
				}
				break;
			case 6:
				{
				_localctx = new BoolExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(163);
				boolLiteral();
				}
				break;
			case 7:
				{
				_localctx = new RealExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(164);
				realLiteral();
				}
				break;
			case 8:
				{
				_localctx = new IntExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(165);
				intLiteral();
				}
				break;
			case 9:
				{
				_localctx = new StringExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(166);
				stringLiteral();
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(209);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,19,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(207);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,18,_ctx) ) {
					case 1:
						{
						_localctx = new MulDivModContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(169);
						if (!(precpred(_ctx, 19))) throw new FailedPredicateException(this, "precpred(_ctx, 19)");
						setState(170);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 15032385536L) != 0)) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(171);
						expression(20);
						}
						break;
					case 2:
						{
						_localctx = new AddSubContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(172);
						if (!(precpred(_ctx, 18))) throw new FailedPredicateException(this, "precpred(_ctx, 18)");
						setState(173);
						_la = _input.LA(1);
						if ( !(_la==PLUS || _la==MINUS) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(174);
						expression(19);
						}
						break;
					case 3:
						{
						_localctx = new ShiftContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(175);
						if (!(precpred(_ctx, 17))) throw new FailedPredicateException(this, "precpred(_ctx, 17)");
						setState(176);
						_la = _input.LA(1);
						if ( !(_la==LSHIFT || _la==RSHIFT) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(177);
						expression(18);
						}
						break;
					case 4:
						{
						_localctx = new BitwiseAndContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(178);
						if (!(precpred(_ctx, 16))) throw new FailedPredicateException(this, "precpred(_ctx, 16)");
						setState(179);
						match(BITAND);
						setState(180);
						expression(17);
						}
						break;
					case 5:
						{
						_localctx = new BitwiseXorContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(181);
						if (!(precpred(_ctx, 15))) throw new FailedPredicateException(this, "precpred(_ctx, 15)");
						setState(182);
						match(BITXOR);
						setState(183);
						expression(16);
						}
						break;
					case 6:
						{
						_localctx = new BitwiseOrContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(184);
						if (!(precpred(_ctx, 14))) throw new FailedPredicateException(this, "precpred(_ctx, 14)");
						setState(185);
						match(BITOR);
						setState(186);
						expression(15);
						}
						break;
					case 7:
						{
						_localctx = new ComparisonContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(187);
						if (!(precpred(_ctx, 13))) throw new FailedPredicateException(this, "precpred(_ctx, 13)");
						setState(188);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 528482304L) != 0)) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(189);
						expression(14);
						}
						break;
					case 8:
						{
						_localctx = new LogicalAndContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(190);
						if (!(precpred(_ctx, 12))) throw new FailedPredicateException(this, "precpred(_ctx, 12)");
						setState(191);
						match(AND);
						setState(192);
						expression(13);
						}
						break;
					case 9:
						{
						_localctx = new LogicalOrContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(193);
						if (!(precpred(_ctx, 11))) throw new FailedPredicateException(this, "precpred(_ctx, 11)");
						setState(194);
						match(OR);
						setState(195);
						expression(12);
						}
						break;
					case 10:
						{
						_localctx = new MethodCallContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(196);
						if (!(precpred(_ctx, 9))) throw new FailedPredicateException(this, "precpred(_ctx, 9)");
						setState(197);
						match(PERIOD);
						setState(198);
						match(ID);
						setState(199);
						match(LPAREN);
						setState(201);
						_errHandler.sync(this);
						_la = _input.LA(1);
						if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 8445487327756480L) != 0)) {
							{
							setState(200);
							parameterList();
							}
						}

						setState(203);
						match(RPAREN);
						}
						break;
					case 11:
						{
						_localctx = new FieldAccessContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(204);
						if (!(precpred(_ctx, 8))) throw new FailedPredicateException(this, "precpred(_ctx, 8)");
						setState(205);
						match(PERIOD);
						setState(206);
						match(ID);
						}
						break;
					}
					} 
				}
				setState(211);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,19,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class IntLiteralContext extends ParserRuleContext {
		public TerminalNode INTEGER() { return getToken(TypedGMLParser.INTEGER, 0); }
		public IntLiteralContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_intLiteral; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterIntLiteral(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitIntLiteral(this);
		}
	}

	public final IntLiteralContext intLiteral() throws RecognitionException {
		IntLiteralContext _localctx = new IntLiteralContext(_ctx, getState());
		enterRule(_localctx, 26, RULE_intLiteral);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(212);
			match(INTEGER);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class RealLiteralContext extends ParserRuleContext {
		public TerminalNode REAL() { return getToken(TypedGMLParser.REAL, 0); }
		public RealLiteralContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_realLiteral; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterRealLiteral(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitRealLiteral(this);
		}
	}

	public final RealLiteralContext realLiteral() throws RecognitionException {
		RealLiteralContext _localctx = new RealLiteralContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_realLiteral);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(214);
			match(REAL);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class StringLiteralContext extends ParserRuleContext {
		public TerminalNode STRING_LITERAL() { return getToken(TypedGMLParser.STRING_LITERAL, 0); }
		public StringLiteralContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_stringLiteral; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterStringLiteral(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitStringLiteral(this);
		}
	}

	public final StringLiteralContext stringLiteral() throws RecognitionException {
		StringLiteralContext _localctx = new StringLiteralContext(_ctx, getState());
		enterRule(_localctx, 30, RULE_stringLiteral);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(216);
			match(STRING_LITERAL);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class BoolLiteralContext extends ParserRuleContext {
		public TerminalNode TRUE() { return getToken(TypedGMLParser.TRUE, 0); }
		public TerminalNode FALSE() { return getToken(TypedGMLParser.FALSE, 0); }
		public BoolLiteralContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_boolLiteral; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).enterBoolLiteral(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof TypedGMLListener ) ((TypedGMLListener)listener).exitBoolLiteral(this);
		}
	}

	public final BoolLiteralContext boolLiteral() throws RecognitionException {
		BoolLiteralContext _localctx = new BoolLiteralContext(_ctx, getState());
		enterRule(_localctx, 32, RULE_boolLiteral);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(218);
			_la = _input.LA(1);
			if ( !(_la==TRUE || _la==FALSE) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 12:
			return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 19);
		case 1:
			return precpred(_ctx, 18);
		case 2:
			return precpred(_ctx, 17);
		case 3:
			return precpred(_ctx, 16);
		case 4:
			return precpred(_ctx, 15);
		case 5:
			return precpred(_ctx, 14);
		case 6:
			return precpred(_ctx, 13);
		case 7:
			return precpred(_ctx, 12);
		case 8:
			return precpred(_ctx, 11);
		case 9:
			return precpred(_ctx, 9);
		case 10:
			return precpred(_ctx, 8);
		}
		return true;
	}

	public static final String _serializedATN =
		"\u0004\u00017\u00dd\u0002\u0000\u0007\u0000\u0002\u0001\u0007\u0001\u0002"+
		"\u0002\u0007\u0002\u0002\u0003\u0007\u0003\u0002\u0004\u0007\u0004\u0002"+
		"\u0005\u0007\u0005\u0002\u0006\u0007\u0006\u0002\u0007\u0007\u0007\u0002"+
		"\b\u0007\b\u0002\t\u0007\t\u0002\n\u0007\n\u0002\u000b\u0007\u000b\u0002"+
		"\f\u0007\f\u0002\r\u0007\r\u0002\u000e\u0007\u000e\u0002\u000f\u0007\u000f"+
		"\u0002\u0010\u0007\u0010\u0001\u0000\u0005\u0000$\b\u0000\n\u0000\f\u0000"+
		"\'\t\u0000\u0001\u0000\u0004\u0000*\b\u0000\u000b\u0000\f\u0000+\u0001"+
		"\u0000\u0005\u0000/\b\u0000\n\u0000\f\u00002\t\u0000\u0001\u0001\u0001"+
		"\u0001\u0001\u0001\u0005\u00017\b\u0001\n\u0001\f\u0001:\t\u0001\u0001"+
		"\u0002\u0001\u0002\u0001\u0002\u0001\u0002\u0001\u0003\u0001\u0003\u0001"+
		"\u0003\u0001\u0003\u0001\u0004\u0005\u0004E\b\u0004\n\u0004\f\u0004H\t"+
		"\u0004\u0001\u0004\u0001\u0004\u0001\u0004\u0003\u0004M\b\u0004\u0001"+
		"\u0004\u0001\u0004\u0001\u0004\u0001\u0004\u0005\u0004S\b\u0004\n\u0004"+
		"\f\u0004V\t\u0004\u0001\u0004\u0001\u0004\u0001\u0005\u0005\u0005[\b\u0005"+
		"\n\u0005\f\u0005^\t\u0005\u0001\u0005\u0001\u0005\u0001\u0005\u0001\u0005"+
		"\u0001\u0005\u0003\u0005e\b\u0005\u0001\u0005\u0001\u0005\u0001\u0006"+
		"\u0001\u0006\u0001\u0006\u0001\u0006\u0001\u0006\u0001\u0006\u0001\u0007"+
		"\u0001\u0007\u0001\b\u0001\b\u0003\bs\b\b\u0001\b\u0001\b\u0001\b\u0001"+
		"\b\u0003\by\b\b\u0001\b\u0001\b\u0001\b\u0001\b\u0003\b\u007f\b\b\u0003"+
		"\b\u0081\b\b\u0001\t\u0001\t\u0001\n\u0001\n\u0001\u000b\u0001\u000b\u0001"+
		"\u000b\u0005\u000b\u008a\b\u000b\n\u000b\f\u000b\u008d\t\u000b\u0001\f"+
		"\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0003\f\u0096\b\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0003\f\u009c\b\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0003\f\u00a8\b\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0003\f\u00ca\b\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0005\f\u00d0\b\f\n\f\f\f\u00d3\t\f\u0001\r\u0001\r\u0001\u000e"+
		"\u0001\u000e\u0001\u000f\u0001\u000f\u0001\u0010\u0001\u0010\u0001\u0010"+
		"\u0000\u0001\u0018\u0011\u0000\u0002\u0004\u0006\b\n\f\u000e\u0010\u0012"+
		"\u0014\u0016\u0018\u001a\u001c\u001e \u0000\b\u0001\u0000\b\n\u0001\u0000"+
		"\u000f\u0010\u0003\u0000\u0015\u0015\u001e\u001e%%\u0001\u0000\u001f!"+
		"\u0001\u0000\u001d\u001e\u0001\u0000&\'\u0001\u0000\u0017\u001c\u0001"+
		"\u0000\u0006\u0007\u00f1\u0000%\u0001\u0000\u0000\u0000\u00023\u0001\u0000"+
		"\u0000\u0000\u0004;\u0001\u0000\u0000\u0000\u0006?\u0001\u0000\u0000\u0000"+
		"\bF\u0001\u0000\u0000\u0000\n\\\u0001\u0000\u0000\u0000\fh\u0001\u0000"+
		"\u0000\u0000\u000en\u0001\u0000\u0000\u0000\u0010\u0080\u0001\u0000\u0000"+
		"\u0000\u0012\u0082\u0001\u0000\u0000\u0000\u0014\u0084\u0001\u0000\u0000"+
		"\u0000\u0016\u0086\u0001\u0000\u0000\u0000\u0018\u00a7\u0001\u0000\u0000"+
		"\u0000\u001a\u00d4\u0001\u0000\u0000\u0000\u001c\u00d6\u0001\u0000\u0000"+
		"\u0000\u001e\u00d8\u0001\u0000\u0000\u0000 \u00da\u0001\u0000\u0000\u0000"+
		"\"$\u0003\u0004\u0002\u0000#\"\u0001\u0000\u0000\u0000$\'\u0001\u0000"+
		"\u0000\u0000%#\u0001\u0000\u0000\u0000%&\u0001\u0000\u0000\u0000&)\u0001"+
		"\u0000\u0000\u0000\'%\u0001\u0000\u0000\u0000(*\u0003\u0006\u0003\u0000"+
		")(\u0001\u0000\u0000\u0000*+\u0001\u0000\u0000\u0000+)\u0001\u0000\u0000"+
		"\u0000+,\u0001\u0000\u0000\u0000,0\u0001\u0000\u0000\u0000-/\u0003\b\u0004"+
		"\u0000.-\u0001\u0000\u0000\u0000/2\u0001\u0000\u0000\u00000.\u0001\u0000"+
		"\u0000\u000001\u0001\u0000\u0000\u00001\u0001\u0001\u0000\u0000\u0000"+
		"20\u0001\u0000\u0000\u000038\u00054\u0000\u000045\u0005+\u0000\u00005"+
		"7\u00054\u0000\u000064\u0001\u0000\u0000\u00007:\u0001\u0000\u0000\u0000"+
		"86\u0001\u0000\u0000\u000089\u0001\u0000\u0000\u00009\u0003\u0001\u0000"+
		"\u0000\u0000:8\u0001\u0000\u0000\u0000;<\u0005\u0001\u0000\u0000<=\u0003"+
		"\u0002\u0001\u0000=>\u0005.\u0000\u0000>\u0005\u0001\u0000\u0000\u0000"+
		"?@\u0005\u0002\u0000\u0000@A\u0003\u0002\u0001\u0000AB\u0005.\u0000\u0000"+
		"B\u0007\u0001\u0000\u0000\u0000CE\u0003\f\u0006\u0000DC\u0001\u0000\u0000"+
		"\u0000EH\u0001\u0000\u0000\u0000FD\u0001\u0000\u0000\u0000FG\u0001\u0000"+
		"\u0000\u0000GI\u0001\u0000\u0000\u0000HF\u0001\u0000\u0000\u0000IL\u0003"+
		"\u000e\u0007\u0000JM\u0003\u0012\t\u0000KM\u0003\u0014\n\u0000LJ\u0001"+
		"\u0000\u0000\u0000LK\u0001\u0000\u0000\u0000LM\u0001\u0000\u0000\u0000"+
		"MN\u0001\u0000\u0000\u0000NO\u0005\u0003\u0000\u0000OP\u00054\u0000\u0000"+
		"PT\u0005,\u0000\u0000QS\u0003\n\u0005\u0000RQ\u0001\u0000\u0000\u0000"+
		"SV\u0001\u0000\u0000\u0000TR\u0001\u0000\u0000\u0000TU\u0001\u0000\u0000"+
		"\u0000UW\u0001\u0000\u0000\u0000VT\u0001\u0000\u0000\u0000WX\u0005-\u0000"+
		"\u0000X\t\u0001\u0000\u0000\u0000Y[\u0003\f\u0006\u0000ZY\u0001\u0000"+
		"\u0000\u0000[^\u0001\u0000\u0000\u0000\\Z\u0001\u0000\u0000\u0000\\]\u0001"+
		"\u0000\u0000\u0000]_\u0001\u0000\u0000\u0000^\\\u0001\u0000\u0000\u0000"+
		"_`\u0003\u0010\b\u0000`a\u0003\u0002\u0001\u0000ad\u00054\u0000\u0000"+
		"bc\u0005\u0016\u0000\u0000ce\u0003\u0018\f\u0000db\u0001\u0000\u0000\u0000"+
		"de\u0001\u0000\u0000\u0000ef\u0001\u0000\u0000\u0000fg\u0005.\u0000\u0000"+
		"g\u000b\u0001\u0000\u0000\u0000hi\u00050\u0000\u0000ij\u0003\u0002\u0001"+
		"\u0000jk\u0005(\u0000\u0000kl\u0003\u0016\u000b\u0000lm\u0005)\u0000\u0000"+
		"m\r\u0001\u0000\u0000\u0000no\u0007\u0000\u0000\u0000o\u000f\u0001\u0000"+
		"\u0000\u0000pr\u0003\u000e\u0007\u0000qs\u0003\u0012\t\u0000rq\u0001\u0000"+
		"\u0000\u0000rs\u0001\u0000\u0000\u0000st\u0001\u0000\u0000\u0000tu\u0005"+
		"\u000b\u0000\u0000u\u0081\u0001\u0000\u0000\u0000vx\u0003\u000e\u0007"+
		"\u0000wy\u0003\u0012\t\u0000xw\u0001\u0000\u0000\u0000xy\u0001\u0000\u0000"+
		"\u0000yz\u0001\u0000\u0000\u0000z{\u0005\r\u0000\u0000{\u0081\u0001\u0000"+
		"\u0000\u0000|~\u0003\u000e\u0007\u0000}\u007f\u0003\u0012\t\u0000~}\u0001"+
		"\u0000\u0000\u0000~\u007f\u0001\u0000\u0000\u0000\u007f\u0081\u0001\u0000"+
		"\u0000\u0000\u0080p\u0001\u0000\u0000\u0000\u0080v\u0001\u0000\u0000\u0000"+
		"\u0080|\u0001\u0000\u0000\u0000\u0081\u0011\u0001\u0000\u0000\u0000\u0082"+
		"\u0083\u0005\f\u0000\u0000\u0083\u0013\u0001\u0000\u0000\u0000\u0084\u0085"+
		"\u0007\u0001\u0000\u0000\u0085\u0015\u0001\u0000\u0000\u0000\u0086\u008b"+
		"\u0003\u0018\f\u0000\u0087\u0088\u0005*\u0000\u0000\u0088\u008a\u0003"+
		"\u0018\f\u0000\u0089\u0087\u0001\u0000\u0000\u0000\u008a\u008d\u0001\u0000"+
		"\u0000\u0000\u008b\u0089\u0001\u0000\u0000\u0000\u008b\u008c\u0001\u0000"+
		"\u0000\u0000\u008c\u0017\u0001\u0000\u0000\u0000\u008d\u008b\u0001\u0000"+
		"\u0000\u0000\u008e\u008f\u0006\f\uffff\uffff\u0000\u008f\u0090\u0007\u0002"+
		"\u0000\u0000\u0090\u00a8\u0003\u0018\f\u0014\u0091\u0092\u0005\u000e\u0000"+
		"\u0000\u0092\u0093\u00054\u0000\u0000\u0093\u0095\u0005(\u0000\u0000\u0094"+
		"\u0096\u0003\u0016\u000b\u0000\u0095\u0094\u0001\u0000\u0000\u0000\u0095"+
		"\u0096\u0001\u0000\u0000\u0000\u0096\u0097\u0001\u0000\u0000\u0000\u0097"+
		"\u00a8\u0005)\u0000\u0000\u0098\u0099\u00054\u0000\u0000\u0099\u009b\u0005"+
		"(\u0000\u0000\u009a\u009c\u0003\u0016\u000b\u0000\u009b\u009a\u0001\u0000"+
		"\u0000\u0000\u009b\u009c\u0001\u0000\u0000\u0000\u009c\u009d\u0001\u0000"+
		"\u0000\u0000\u009d\u00a8\u0005)\u0000\u0000\u009e\u009f\u0005(\u0000\u0000"+
		"\u009f\u00a0\u0003\u0018\f\u0000\u00a0\u00a1\u0005)\u0000\u0000\u00a1"+
		"\u00a8\u0001\u0000\u0000\u0000\u00a2\u00a8\u00054\u0000\u0000\u00a3\u00a8"+
		"\u0003 \u0010\u0000\u00a4\u00a8\u0003\u001c\u000e\u0000\u00a5\u00a8\u0003"+
		"\u001a\r\u0000\u00a6\u00a8\u0003\u001e\u000f\u0000\u00a7\u008e\u0001\u0000"+
		"\u0000\u0000\u00a7\u0091\u0001\u0000\u0000\u0000\u00a7\u0098\u0001\u0000"+
		"\u0000\u0000\u00a7\u009e\u0001\u0000\u0000\u0000\u00a7\u00a2\u0001\u0000"+
		"\u0000\u0000\u00a7\u00a3\u0001\u0000\u0000\u0000\u00a7\u00a4\u0001\u0000"+
		"\u0000\u0000\u00a7\u00a5\u0001\u0000\u0000\u0000\u00a7\u00a6\u0001\u0000"+
		"\u0000\u0000\u00a8\u00d1\u0001\u0000\u0000\u0000\u00a9\u00aa\n\u0013\u0000"+
		"\u0000\u00aa\u00ab\u0007\u0003\u0000\u0000\u00ab\u00d0\u0003\u0018\f\u0014"+
		"\u00ac\u00ad\n\u0012\u0000\u0000\u00ad\u00ae\u0007\u0004\u0000\u0000\u00ae"+
		"\u00d0\u0003\u0018\f\u0013\u00af\u00b0\n\u0011\u0000\u0000\u00b0\u00b1"+
		"\u0007\u0005\u0000\u0000\u00b1\u00d0\u0003\u0018\f\u0012\u00b2\u00b3\n"+
		"\u0010\u0000\u0000\u00b3\u00b4\u0005\"\u0000\u0000\u00b4\u00d0\u0003\u0018"+
		"\f\u0011\u00b5\u00b6\n\u000f\u0000\u0000\u00b6\u00b7\u0005$\u0000\u0000"+
		"\u00b7\u00d0\u0003\u0018\f\u0010\u00b8\u00b9\n\u000e\u0000\u0000\u00b9"+
		"\u00ba\u0005#\u0000\u0000\u00ba\u00d0\u0003\u0018\f\u000f\u00bb\u00bc"+
		"\n\r\u0000\u0000\u00bc\u00bd\u0007\u0006\u0000\u0000\u00bd\u00d0\u0003"+
		"\u0018\f\u000e\u00be\u00bf\n\f\u0000\u0000\u00bf\u00c0\u0005\u0013\u0000"+
		"\u0000\u00c0\u00d0\u0003\u0018\f\r\u00c1\u00c2\n\u000b\u0000\u0000\u00c2"+
		"\u00c3\u0005\u0014\u0000\u0000\u00c3\u00d0\u0003\u0018\f\f\u00c4\u00c5"+
		"\n\t\u0000\u0000\u00c5\u00c6\u0005+\u0000\u0000\u00c6\u00c7\u00054\u0000"+
		"\u0000\u00c7\u00c9\u0005(\u0000\u0000\u00c8\u00ca\u0003\u0016\u000b\u0000"+
		"\u00c9\u00c8\u0001\u0000\u0000\u0000\u00c9\u00ca\u0001\u0000\u0000\u0000"+
		"\u00ca\u00cb\u0001\u0000\u0000\u0000\u00cb\u00d0\u0005)\u0000\u0000\u00cc"+
		"\u00cd\n\b\u0000\u0000\u00cd\u00ce\u0005+\u0000\u0000\u00ce\u00d0\u0005"+
		"4\u0000\u0000\u00cf\u00a9\u0001\u0000\u0000\u0000\u00cf\u00ac\u0001\u0000"+
		"\u0000\u0000\u00cf\u00af\u0001\u0000\u0000\u0000\u00cf\u00b2\u0001\u0000"+
		"\u0000\u0000\u00cf\u00b5\u0001\u0000\u0000\u0000\u00cf\u00b8\u0001\u0000"+
		"\u0000\u0000\u00cf\u00bb\u0001\u0000\u0000\u0000\u00cf\u00be\u0001\u0000"+
		"\u0000\u0000\u00cf\u00c1\u0001\u0000\u0000\u0000\u00cf\u00c4\u0001\u0000"+
		"\u0000\u0000\u00cf\u00cc\u0001\u0000\u0000\u0000\u00d0\u00d3\u0001\u0000"+
		"\u0000\u0000\u00d1\u00cf\u0001\u0000\u0000\u0000\u00d1\u00d2\u0001\u0000"+
		"\u0000\u0000\u00d2\u0019\u0001\u0000\u0000\u0000\u00d3\u00d1\u0001\u0000"+
		"\u0000\u0000\u00d4\u00d5\u00053\u0000\u0000\u00d5\u001b\u0001\u0000\u0000"+
		"\u0000\u00d6\u00d7\u00052\u0000\u0000\u00d7\u001d\u0001\u0000\u0000\u0000"+
		"\u00d8\u00d9\u00051\u0000\u0000\u00d9\u001f\u0001\u0000\u0000\u0000\u00da"+
		"\u00db\u0007\u0007\u0000\u0000\u00db!\u0001\u0000\u0000\u0000\u0014%+"+
		"08FLT\\drx~\u0080\u008b\u0095\u009b\u00a7\u00c9\u00cf\u00d1";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}