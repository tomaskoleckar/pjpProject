namespace pjpProject;

public class AstBuilder : LanguageBaseVisitor<object?>
{
    public List<Stmt> BuildProgram(LanguageParser.ProgramContext ctx)
        => ctx.statement().Select(s => (Stmt)Visit(s)!).ToList();

    // ── Statements ────────────────────────────────────────────────────────────

    public override object? VisitEmptyStmt(LanguageParser.EmptyStmtContext ctx)
        => new EmptyStmt(ctx.Start.Line);

    public override object? VisitDeclStmt(LanguageParser.DeclStmtContext ctx)
    {
        var vtype = GetVarType(ctx.type());
        var names = ctx.ID().Select(id => id.GetText()).ToList();
        return new DeclStmt(vtype, names, ctx.Start.Line);
    }

    public override object? VisitExprStmt(LanguageParser.ExprStmtContext ctx)
        => new ExprStmt((Expr)Visit(ctx.expr())!, ctx.Start.Line);

    public override object? VisitReadStmt(LanguageParser.ReadStmtContext ctx)
    {
        var names = ctx.ID().Select(id => id.GetText()).ToList();
        return new ReadStmt(names, ctx.Start.Line);
    }

    public override object? VisitWriteStmt(LanguageParser.WriteStmtContext ctx)
    {
        var exprs = ctx.expr().Select(e => (Expr)Visit(e)!).ToList();
        return new WriteStmt(exprs, ctx.Start.Line);
    }

    public override object? VisitBlockStmt(LanguageParser.BlockStmtContext ctx)
    {
        var stmts = ctx.statement().Select(s => (Stmt)Visit(s)!).ToList();
        return new BlockStmt(stmts, ctx.Start.Line);
    }

    public override object? VisitIfStmt(LanguageParser.IfStmtContext ctx)
    {
        var cond     = (Expr)Visit(ctx.expr())!;
        var branches = ctx.statement();
        var then     = (Stmt)Visit(branches[0])!;
        var els      = branches.Length > 1 ? (Stmt)Visit(branches[1])! : null;
        return new IfStmt(cond, then, els, ctx.Start.Line);
    }

    public override object? VisitWhileStmt(LanguageParser.WhileStmtContext ctx)
    {
        var cond = (Expr)Visit(ctx.expr())!;
        var body = (Stmt)Visit(ctx.statement())!;
        return new WhileStmt(cond, body, ctx.Start.Line);
    }

    // ── Expressions ───────────────────────────────────────────────────────────

    public override object? VisitAssignExpr(LanguageParser.AssignExprContext ctx)
    {
        var exprs = ctx.expr();
        var lhs   = (Expr)Visit(exprs[0])!;
        if (lhs is not IdExpr idExpr)
            throw new Exception($"Line {ctx.Start.Line}: left side of '=' must be an identifier");
        var val = (Expr)Visit(exprs[1])!;
        return new AssignExpr(idExpr.Name, val, ctx.Start.Line);
    }

    public override object? VisitMulExpr(LanguageParser.MulExprContext ctx)
        => new BinopExpr(ctx.GetChild(1).GetText(),
               (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitAddExpr(LanguageParser.AddExprContext ctx)
        => new BinopExpr(ctx.GetChild(1).GetText(),
               (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitRelExpr(LanguageParser.RelExprContext ctx)
        => new BinopExpr(ctx.GetChild(1).GetText(),
               (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitEqExpr(LanguageParser.EqExprContext ctx)
        => new BinopExpr(ctx.GetChild(1).GetText(),
               (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitAndExpr(LanguageParser.AndExprContext ctx)
        => new BinopExpr("&&", (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitOrExpr(LanguageParser.OrExprContext ctx)
        => new BinopExpr("||", (Expr)Visit(ctx.expr(0))!, (Expr)Visit(ctx.expr(1))!, ctx.Start.Line);

    public override object? VisitNotExpr(LanguageParser.NotExprContext ctx)
        => new UnopExpr("!", (Expr)Visit(ctx.expr())!, ctx.Start.Line);

    public override object? VisitUnaryMinusExpr(LanguageParser.UnaryMinusExprContext ctx)
        => new UnopExpr("-", (Expr)Visit(ctx.expr())!, ctx.Start.Line);

    public override object? VisitParenExpr(LanguageParser.ParenExprContext ctx)
        => Visit(ctx.expr());

    public override object? VisitIntLit(LanguageParser.IntLitContext ctx)
        => new IntLitExpr(int.Parse(ctx.INT_LIT().GetText()), ctx.Start.Line);

    public override object? VisitFloatLit(LanguageParser.FloatLitContext ctx)
        => new FloatLitExpr(
               double.Parse(ctx.FLOAT_LIT().GetText(),
                   System.Globalization.CultureInfo.InvariantCulture),
               ctx.Start.Line);

    public override object? VisitBoolLit(LanguageParser.BoolLitContext ctx)
        => new BoolLitExpr(ctx.BOOL_LIT().GetText() == "true", ctx.Start.Line);

    public override object? VisitStrLit(LanguageParser.StrLitContext ctx)
    {
        var raw   = ctx.STR_LIT().GetText();
        var value = raw[1..^1].Replace("\\\"", "\"");
        return new StrLitExpr(value, ctx.Start.Line);
    }

    public override object? VisitIdExpr(LanguageParser.IdExprContext ctx)
        => new IdExpr(ctx.ID().GetText(), ctx.Start.Line);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static VarType GetVarType(LanguageParser.TypeContext ctx) =>
        ctx.GetText() switch
        {
            "int"    => VarType.Int,
            "float"  => VarType.Float,
            "bool"   => VarType.Bool,
            "string" => VarType.String,
            var t    => throw new Exception($"Line {ctx.Start.Line}: unknown type '{t}'")
        };
}
