namespace pjpProject;

public enum VarType { Int, Float, Bool, String }

// Statements
public abstract record Stmt(int Line);
public record EmptyStmt(int Line) : Stmt(Line);
public record DeclStmt(VarType VType, List<string> Names, int Line) : Stmt(Line);
public record ExprStmt(Expr Expr, int Line) : Stmt(Line);
public record ReadStmt(List<string> Names, int Line) : Stmt(Line);
public record WriteStmt(List<Expr> Exprs, int Line) : Stmt(Line);
public record BlockStmt(List<Stmt> Stmts, int Line) : Stmt(Line);
public record IfStmt(Expr Cond, Stmt Then, Stmt? Else, int Line) : Stmt(Line);
public record WhileStmt(Expr Cond, Stmt Body, int Line) : Stmt(Line);

// Expressions
public abstract record Expr(int Line);
public record IntLitExpr(int Value, int Line) : Expr(Line);
public record FloatLitExpr(double Value, int Line) : Expr(Line);
public record BoolLitExpr(bool Value, int Line) : Expr(Line);
public record StrLitExpr(string Value, int Line) : Expr(Line);
public record IdExpr(string Name, int Line) : Expr(Line);
public record AssignExpr(string Name, Expr Value, int Line) : Expr(Line);
public record BinopExpr(string Op, Expr Left, Expr Right, int Line) : Expr(Line);
public record UnopExpr(string Op, Expr Operand, int Line) : Expr(Line);
