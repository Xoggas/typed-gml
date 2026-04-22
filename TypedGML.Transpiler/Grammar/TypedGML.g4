grammar TypedGML;

// -------------------------------------------------------------------------------
//  Program
// -------------------------------------------------------------------------------

program
    : usingDecl* namespaceDecl* typeDecl* EOF
    ;

// -------------------------------------------------------------------------------
//  Usings & Namespaces
// -------------------------------------------------------------------------------

usingDecl
    : USING qualifiedName SEMI
    ;

namespaceDecl
    : NAMESPACE qualifiedName SEMI
    ;

// -------------------------------------------------------------------------------
//  Type Declarations
// -------------------------------------------------------------------------------

typeDecl
    : classDecl
    | structDecl
    | enumDecl
    | interfaceDecl
    | delegateDecl
    ;

// public abstract? class Name<T: IConstraint> : Base, IFace { ... }
classDecl
    : decorator*
      accessMod classMod? CLASS ID typeParams?
      inheritanceList?
      LBRACE memberDecl* RBRACE
    ;

// public readonly? struct Name<T> : IFace { ... }
structDecl
    : decorator*
      accessMod READONLY? STRUCT ID typeParams?
      inheritanceList?
      LBRACE memberDecl* RBRACE
    ;

// public enum Name { A = 0, B, C }
enumDecl
    : decorator*
      accessMod ENUM ID
      LBRACE (enumMember (COMMA enumMember)* COMMA?)? RBRACE
    ;

enumMember
    : decorator* nameId (ASSIGN intLiteral)?
    ;

// public interface IName<T> : IBase { ... }
interfaceDecl
    : decorator*
      accessMod INTERFACE ID typeParams?
      inheritanceList?
      LBRACE interfaceMemberDecl* RBRACE
    ;

delegateDecl
    : decorator*
      accessMod DELEGATE typeRef ID typeParams?
      LPAREN paramList? RPAREN
      SEMI
    ;

// -------------------------------------------------------------------------------
//  Generics
//
//  Note: LT / GT tokens are shared with comparison operators.
//        Ambiguity between  List<int>  and  a < b > c  is resolved semantically.
// -------------------------------------------------------------------------------

// <T: IConstraint, U>
typeParams
    : LT typeParam (COMMA typeParam)* GT
    ;

// T: IConstraint   or just  T
typeParam
    : ID (COLON typeRef)?
    ;

// <int, string>   (use-site type arguments)
typeArgs
    : LT typeRef (COMMA typeRef)* GT
    ;

// -------------------------------------------------------------------------------
//  Inheritance
//
//  Single base class + multiple interfaces for classes is enforced semantically.
// -------------------------------------------------------------------------------

inheritanceList
    : COLON typeRef (COMMA typeRef)*
    ;

// -------------------------------------------------------------------------------
//  Type References
// -------------------------------------------------------------------------------

// int[]  /  List<string>[]  /  Map<string, int>
typeRef
    : qualifiedName typeArgs? (LBRACKET RBRACKET)*
    ;

qualifiedName
    : ID (PERIOD ID)*
    ;

// Identifiers that are also contextual keywords.
// Allows: int value = ...; obj.field; obj.get_X(); etc.
nameId
    : ID
    | VALUE
    | FIELD
    | GET
    | SET
    ;

// -------------------------------------------------------------------------------
//  Member Declarations
// -------------------------------------------------------------------------------

memberDecl
    : fieldDecl
    | propertyDecl
    | indexerDecl
    | methodDecl
    | constructorDecl
    | typeDecl
    ;

// public static? readonly? Type name (= expr)?;
fieldDecl
    : decorator*
      fieldModifiers typeRef nameId (ASSIGN expression)? SEMI
    ;

// public static? virtual? Type Name { get { } set { } }
propertyDecl
    : decorator*
      propertyModifiers typeRef nameId
      LBRACE accessorDecl+ RBRACE
    ;

indexerDecl
    : decorator*
      propertyModifiers typeRef nameId
      LBRACKET param RBRACKET
      LBRACE accessorDecl+ RBRACE
    ;

// get { ... }  /  get;  /  private set { ... }  /  set;
// Inside bodies: use `field` for the backing field, `value` for the incoming value.
accessorDecl
    : accessMod? GET (block | ARROW expression SEMI | SEMI)
    | accessMod? SET (block | ARROW expression SEMI | SEMI)
    ;

// public static? virtual? ReturnType Name<T>(params) { ... }
methodDecl
    : decorator*
      methodModifiers typeRef nameId typeParams?
      LPAREN paramList? RPAREN
      (block | SEMI)
    | decorator*
      methodModifiers typeRef OPERATOR overloadableOperator
      LPAREN paramList? RPAREN
      (block | SEMI)
    | decorator*
      methodModifiers (IMPLICIT | EXPLICIT) OPERATOR typeRef
      LPAREN paramList? RPAREN
      (block | SEMI)
    ;

overloadableOperator
    : PLUS
    | MINUS
    | STAR
    | SLASH
    | PERCENT
    | BITAND
    | BITOR
    | BITXOR
    | BITNOT
    | EQ
    | NEQ
    | LT
    | GT
    | LE
    | GE
    | LSHIFT
    | GT GT
    | NOT
    ;

// public constructor(var p: int) : base(p) { ... }
constructorDecl
    : decorator*
      accessMod CONSTRUCTOR
      LPAREN paramList? RPAREN
      (COLON BASE LPAREN argList? RPAREN)?
      block
    ;

// --- Interface members (no access modifier; abstract by definition) -----------

interfaceMemberDecl
    : interfaceMethodDecl
    | interfacePropertyDecl
    ;

interfaceMethodDecl
    : decorator*
      typeRef nameId typeParams?
      LPAREN paramList? RPAREN
      (block | SEMI)
    ;

// An interface property declaration (no access modifier).</summary>
interfacePropertyDecl
    : decorator*
      typeRef nameId
      LBRACE interfaceAccessorDecl+ RBRACE
    ;

interfaceAccessorDecl
    : GET SEMI
    | SET SEMI
    ;

// -------------------------------------------------------------------------------
//  Modifiers
// -------------------------------------------------------------------------------

accessMod
    : PUBLIC
    | PROTECTED
    | PRIVATE
    ;

classMod
    : ABSTRACT
    | SEALED
    | VIRTUAL
    | STATIC
    ;

scopeMod
    : STATIC
    ;

fieldModifiers
    : accessMod STATIC? READONLY
    | accessMod STATIC? CONST
    | accessMod STATIC?
    ;

propertyModifiers
    : accessMod scopeMod? READONLY? (VIRTUAL | ABSTRACT | OVERRIDE | SEALED)?
    ;

methodModifiers
    : accessMod STATIC? (VIRTUAL | ABSTRACT | OVERRIDE | SEALED)?
    ;

// -------------------------------------------------------------------------------
//  Parameters & Arguments
// -------------------------------------------------------------------------------

paramList
    : param (COMMA param)*
    ;

// Type paramName (= defaultValue)?
param
    : decorator* typeRef nameId (ASSIGN expression)?
    ;

argList
    : arg (COMMA arg)*
    ;

arg
    : (nameId COLON)? expression
    ;

// -------------------------------------------------------------------------------
//  Decorators
// -------------------------------------------------------------------------------

// @Decorator  or  @Decorator(arg1, arg2)
decorator
    : AT qualifiedName (LPAREN argList? RPAREN)?
    ;

// -------------------------------------------------------------------------------
//  Statements
// -------------------------------------------------------------------------------

block
    : LBRACE statement* RBRACE
    ;

statement
    : block
    | localVarDecl
    | ifStmt
    | whileStmt
    | forStmt
    | repeatStmt
    | switchStmt
    | withStmt
    | returnStmt
    | breakStmt
    | continueStmt
    | tryStmt
    | rawStmt
    | expressionStmt
    ;

// int x = 5;
localVarDecl
    : typeRef nameId (ASSIGN expression)? SEMI
    ;

expressionStmt
    : expression SEMI
    ;

ifStmt
    : IF LPAREN expression RPAREN block
      (ELSE IF LPAREN expression RPAREN block)*
      (ELSE block)?
    ;

whileStmt
    : WHILE LPAREN expression RPAREN block
    ;

// for (int i = 0; i < 10; i = i + 1) { ... }
forStmt
    : FOR LPAREN forInit SEMI expression? SEMI forUpdate? RPAREN block
    ;

forInit
    : typeRef nameId (ASSIGN expression)?
    | expression
    |
    ;

forUpdate
    : expression (COMMA expression)*
    ;

// repeat (n) { ... }  — shorthand for a counted loop; n is the iteration count
repeatStmt
    : REPEAT LPAREN expression RPAREN block
    ;

switchStmt
    : SWITCH LPAREN expression RPAREN LBRACE switchSection* RBRACE
    ;

switchSection
    : (CASE expression | DEFAULT) COLON statement*
    ;

// with (TypeName varName) { ... }  — GML-style instance iteration with a typed var
withStmt
    : WITH LPAREN typeRef nameId RPAREN block
    ;

returnStmt
    : RETURN expression? SEMI
    ;

breakStmt
    : BREAK SEMI
    ;

continueStmt
    : CONTINUE SEMI
    ;

tryStmt
    : TRY block catchClause* finallyClause?
    ;

catchClause
    : CATCH LPAREN typeRef nameId RPAREN block
    ;

finallyClause
    : FINALLY block
    ;

// Every line beginning with '#' is emitted verbatim into the output (raw GML).
rawStmt
    : RAW_LINE
    ;

// -------------------------------------------------------------------------------
//  Expressions  — listed highest precedence first
//
//  Known ambiguities (resolved semantically):
//    • LT / GT are used for both generics and comparison
//    • LPAREN typeRef RPAREN expression  vs  LPAREN expression RPAREN
// -------------------------------------------------------------------------------

expression
    // -- Postfix ---------------------------------------------------------------
    : expression LPAREN argList? RPAREN                                   # invokeExpr
    | expression PERIOD nameId LPAREN argList? RPAREN                     # methodCallExpr
    | expression PERIOD nameId                                            # fieldAccessExpr
    | expression LBRACKET expression RBRACKET                         # indexExpr

    // -- Unary prefix ----------------------------------------------------------
    | (MINUS | BITNOT | NOT) expression                               # unaryExpr

    // -- Cast  (ambiguous with parenExpr; resolved semantically) ---------------
    | LPAREN typeRef RPAREN expression                                 # castExpr

    // -- Multiplicative --------------------------------------------------------
    | expression (STAR | SLASH | PERCENT) expression                  # mulDivMod

    // -- Additive --------------------------------------------------------------
    | expression (PLUS | MINUS) expression                            # addSub

    // -- Bitwise shift ---------------------------------------------------------
    // Right-shift uses GT GT (two tokens) so that >> never conflicts with
    // nested closing angle brackets in generic types, e.g. List<List<T>>.
    | expression LSHIFT expression                                    # leftShiftExpr
    | expression GT GT expression                                     # rightShiftExpr

    // -- Bitwise ---------------------------------------------------------------
    | expression BITAND expression                                     # bitwiseAnd
    | expression BITXOR expression                                     # bitwiseXor
    | expression BITOR expression                                      # bitwiseOr

    // -- Relational & type checks ----------------------------------------------
    | expression (EQ | NEQ | LT | GT | LE | GE) expression           # comparison
    | expression IS typeRef                                           # isExpr
    | expression AS typeRef                                           # asExpr

    // -- Logical ---------------------------------------------------------------
    | expression AND expression                                       # logicalAnd
    | expression OR expression                                        # logicalOr

    // -- Ternary (right-associative) -------------------------------------------
    | <assoc=right> expression QUESTION expression COLON expression   # ternaryExpr

    // -- Assignment (right-associative) ----------------------------------------
    | <assoc=right> expression
        ( ASSIGN
        | PLUS_ASSIGN | MINUS_ASSIGN
        | STAR_ASSIGN | SLASH_ASSIGN
        | PERCENT_ASSIGN
        ) expression                                                  # assignExpr

    // -- Object / array creation -----------------------------------------------
    | NEW typeRef LPAREN argList? RPAREN                              # newObjectExpr
    | NEW typeRef LBRACKET expression RBRACKET                        # newArrayExpr
    | NEW LPAREN argList? RPAREN                                      # newImplicitExpr

    // -- Intrinsics ------------------------------------------------------------
    | TYPEOF LPAREN typeRef RPAREN                                    # typeofExpr
    | NAMEOF LPAREN expression RPAREN                                 # nameofExpr
    | DEFAULT LPAREN typeRef RPAREN                                   # defaultOfExpr

    // -- Base access -----------------------------------------------------------
    | BASE PERIOD nameId LPAREN argList? RPAREN                           # baseCallExpr
    | BASE PERIOD nameId                                                  # baseAccessExpr

    // -- Lambda ----------------------------------------------------------------
    | lambdaExpr                                                      # lambdaExprAtom

    // -- Array initializer -----------------------------------------------------
    | LBRACKET (expression (COMMA expression)*)? RBRACKET             # arrayInitExpr

    // -- Primary ---------------------------------------------------------------
    | nameId LPAREN argList? RPAREN                                       # funcCallExpr
    | LPAREN expression RPAREN                                        # parenExpr
    | FIELD                                                           # fieldKeywordExpr
    | VALUE                                                           # valueKeywordExpr
    | ID                                                              # idExpr
    | NULL                                                            # nullExpr
    | DEFAULT                                                         # defaultExpr
    | boolLiteral                                                     # boolExpr
    | realLiteral                                                     # realExpr
    | intLiteral                                                      # intExpr
    | stringLiteral                                                   # stringExpr
    ;

// (T1 p1, T2 p2) => expr   or   id => expr
lambdaExpr
    : LPAREN paramList? RPAREN ARROW (expression | block)
    | nameId ARROW (expression | block)
    ;

// -------------------------------------------------------------------------------
//  Literals
// -------------------------------------------------------------------------------

intLiteral    : INTEGER | HEX_LITERAL | BIN_LITERAL ;
realLiteral   : REAL           ;
stringLiteral : STRING_LITERAL ;
boolLiteral   : TRUE | FALSE   ;

// -------------------------------------------------------------------------------
//  Keywords
// -------------------------------------------------------------------------------

// -- Type declarations ---------------------------------------------------------
CLASS     : 'class'     ;
STRUCT    : 'struct'    ;
ENUM      : 'enum'      ;
INTERFACE : 'interface' ;

// -- Access --------------------------------------------------------------------
PUBLIC    : 'public'    ;
PROTECTED : 'protected' ;
PRIVATE   : 'private'   ;

// -- Modifiers -----------------------------------------------------------------
STATIC    : 'static'   ;
GLOBAL    : 'global'   ;
READONLY  : 'readonly' ;
CONST     : 'const'    ;
ABSTRACT  : 'abstract' ;
SEALED    : 'sealed'   ;
VIRTUAL   : 'virtual'  ;
OVERRIDE  : 'override' ;

// -- Declaration keywords ------------------------------------------------------
CONSTRUCTOR : 'constructor' ;
OPERATOR    : 'operator'    ;
IMPLICIT    : 'implicit'    ;
EXPLICIT    : 'explicit'    ;
DELEGATE    : 'delegate'    ;

// -- Type operations -----------------------------------------------------------
NEW    : 'new'    ;
NULL   : 'null'   ;
IS     : 'is'     ;
AS     : 'as'     ;
TYPEOF : 'typeof' ;
NAMEOF : 'nameof' ;
BASE   : 'base'   ;

// -- Flow control --------------------------------------------------------------
IF       : 'if'       ;
ELSE     : 'else'     ;
WHILE    : 'while'    ;
FOR      : 'for'      ;
REPEAT   : 'repeat'   ;
SWITCH   : 'switch'   ;
CASE     : 'case'     ;
DEFAULT  : 'default'  ;
BREAK    : 'break'    ;
CONTINUE : 'continue' ;
RETURN   : 'return'   ;
WITH     : 'with'     ;

// -- Error handling ------------------------------------------------------------
TRY     : 'try'     ;
CATCH   : 'catch'   ;
FINALLY : 'finally' ;

// -- Boolean -------------------------------------------------------------------
TRUE  : 'true'  ;
FALSE : 'false' ;

// -- Logical (word operators instead of symbols) -------------------------------
AND : 'and' ;
OR  : 'or'  ;
NOT : 'not' ;

// -- Property accessor keywords ------------------------------------------------
// `get` / `set` mark accessors; `field` = backing field; `value` = incoming value
GET   : 'get'   ;
SET   : 'set'   ;
FIELD : 'field' ;
VALUE : 'value' ;

// -- Modularity ----------------------------------------------------------------
USING     : 'using'     ;
NAMESPACE : 'namespace' ;

// -------------------------------------------------------------------------------
//  Operators
//  (Longer tokens must appear before their prefixes for correct maximal munch.)
// -------------------------------------------------------------------------------

// -- Compound assignment -------------------------------------------------------
PLUS_ASSIGN    : '+=' ;
MINUS_ASSIGN   : '-=' ;
STAR_ASSIGN    : '*=' ;
SLASH_ASSIGN   : '/=' ;
PERCENT_ASSIGN : '%=' ;

// -- Equality & relational (multi-char before single-char) ---------------------
EQ  : '==' ;
NEQ : '!=' ;
LE  : '<=' ;
GE  : '>=' ;

// -- Arrow (before ASSIGN and GT) ----------------------------------------------
ARROW : '=>' ;

// -- Assignment (after == and compound forms) ----------------------------------
ASSIGN : '=' ;

// -- Relational (after <= and >=) ----------------------------------------------
LT : '<' ;
GT : '>' ;

// -- Arithmetic ----------------------------------------------------------------
PLUS    : '+' ;
MINUS   : '-' ;
STAR    : '*' ;
SLASH   : '/' ;
PERCENT : '%' ;

// -- Bitwise (longer before shorter) ------------------------------------------
// Note: RSHIFT (>>) is intentionally absent — right-shift is expressed as GT GT
// in the parser so that >> does not consume both closing > of nested generics.
LSHIFT : '<<' ;
BITAND : '&'  ;
BITOR  : '|'  ;
BITXOR : '^'  ;
BITNOT : '~' ;

// -- Other ---------------------------------------------------------------------
QUESTION : '?' ;

// -------------------------------------------------------------------------------
//  Delimiters
// -------------------------------------------------------------------------------

LPAREN   : '(' ;
RPAREN   : ')' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
LBRACE   : '{' ;
RBRACE   : '}' ;
COMMA    : ',' ;
PERIOD   : '.' ;
SEMI     : ';' ;
COLON    : ':' ;
AT       : '@' ;

// -------------------------------------------------------------------------------
//  Literals & Identifiers
// -------------------------------------------------------------------------------


STRING_LITERAL
    : '"' (ESC_SEQ | ~["\\\r\n])* '"'
    ;

fragment ESC_SEQ
    : '\\' ["\\'bfnrt]
    | '\\u' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F]
    ;

HEX_LITERAL : '0' [xX] [0-9a-fA-F]+ ;
BIN_LITERAL : '0' [bB] [01]+         ;
REAL        : [0-9]+ '.' [0-9]+      ;
INTEGER     : [0-9]+                 ;

ID : [a-zA-Z_][a-zA-Z_0-9]* ;

// -------------------------------------------------------------------------------
//  Raw GML lines  — '#...' is kept verbatim in the parse tree and emitted as-is
// -------------------------------------------------------------------------------

RAW_LINE : '#' ~[\r\n]* ;

// -------------------------------------------------------------------------------
//  Whitespace & Comments
// -------------------------------------------------------------------------------

WS            : [ \t\n\r\f]+ -> skip ;
LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
