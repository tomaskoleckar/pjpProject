grammar Language;

program : statement* EOF ;

statement
    : ';'                                                   # emptyStmt
    | type ID (',' ID)* ';'                                 # declStmt
    | expr ';'                                              # exprStmt
    | READ ID (',' ID)* ';'                                 # readStmt
    | WRITE expr (',' expr)* ';'                            # writeStmt
    | '{' statement* '}'                                    # blockStmt
    | IF '(' expr ')' statement (ELSE statement)?           # ifStmt
    | WHILE '(' expr ')' statement                          # whileStmt
    ;

type : INT | FLOAT | BOOL | STRING ;

expr
    : expr ('*' | '/' | '%') expr                           # mulExpr
    | expr ('+' | '-' | '.') expr                           # addExpr
    | expr ('<' | '>') expr                                 # relExpr
    | expr ('==' | '!=') expr                               # eqExpr
    | expr '&&' expr                                        # andExpr
    | expr '||' expr                                        # orExpr
    | <assoc=right> expr '=' expr                           # assignExpr
    | '!' expr                                              # notExpr
    | '-' expr                                              # unaryMinusExpr
    | '(' expr ')'                                          # parenExpr
    | INT_LIT                                               # intLit
    | FLOAT_LIT                                             # floatLit
    | BOOL_LIT                                              # boolLit
    | STR_LIT                                               # strLit
    | ID                                                    # idExpr
    ;

// Keywords
INT    : 'int' ;
FLOAT  : 'float' ;
BOOL   : 'bool' ;
STRING : 'string' ;
READ   : 'read' ;
WRITE  : 'write' ;
IF     : 'if' ;
ELSE   : 'else' ;
WHILE  : 'while' ;

BOOL_LIT : 'true' | 'false' ;

FLOAT_LIT : [0-9]+ '.' [0-9]* | [0-9]* '.' [0-9]+ ;
INT_LIT   : [0-9]+ ;
STR_LIT   : '"' (~["\r\n] | '\\"')* '"' ;

ID : [a-zA-Z][a-zA-Z0-9]* ;

LINE_COMMENT : '//' ~[\r\n]* -> skip ;
WS           : [ \t\r\n]+ -> skip ;
