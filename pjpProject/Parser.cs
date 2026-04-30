namespace pjpProject;

public class LegacyParser
{
    private readonly List<Token> _tokens;
    private int _pos;
    private readonly List<string> _errors = new();

    public LegacyParser(List<Token> tokens) => _tokens = tokens;

    public List<string> Errors => _errors;

    private Token Current => _tokens[_pos];
    private Token Peek(int offset = 1) => _tokens[Math.Min(_pos + offset, _tokens.Count - 1)];

    private Token Consume()
    {
        var t = _tokens[_pos];
        if (_pos < _tokens.Count - 1) _pos++;
        return t;
    }

    private Token Expect(TokenType tt)
    {
        if (Current.Type != tt)
            _errors.Add($"Line {Current.Line}: expected '{tt}', got '{Current.Text}'");
        return Consume();
    }

    private bool Check(TokenType tt) => Current.Type == tt;

    private bool Match(TokenType tt)
    {
        if (Check(tt)) { Consume(); return true; }
        return false;
    }

    public List<Stmt> Parse()
    {
        var stmts = new List<Stmt>();
        while (!Check(TokenType.Eof))
        {
            try { stmts.Add(ParseStatement()); }
            catch (Exception ex)
            {
                _errors.Add(ex.Message);
                Synchronize();
            }
        }
        return stmts;
    }

    private void Synchronize()
    {
        while (!Check(TokenType.Eof) && !Check(TokenType.Semi) &&
               !Check(TokenType.RBrace)) Consume();
        if (Check(TokenType.Semi)) Consume();
    }
    
    private VarType MapToArrayType(VarType baseType, int line) => baseType switch
    {
        VarType.Int   => VarType.IntArray,
        VarType.Float => VarType.FloatArray,
        VarType.Bool => VarType.BoolArray,
        VarType.String => VarType.StringArray,
        _ => throw new Exception($"Line {line}: Type {baseType} cannot be an array.")
    };

    private Stmt ParseStatement()
    {
        int line = Current.Line;

        if (Check(TokenType.Semi)) { Consume(); return new EmptyStmt(line); }

        if (IsType(Current.Type))
        {
            var vtype = ParseType();
            
            if (Match(TokenType.LBracket))
            {
                Expect(TokenType.RBracket);
                // Map to your new VarType.IntArray or similar
                vtype = MapToArrayType(vtype, line); 
            }
            
            var names = new List<string> { Expect(TokenType.Id).Text };
            while (Match(TokenType.Comma)) names.Add(Expect(TokenType.Id).Text);
            Expect(TokenType.Semi);
            return new DeclStmt(vtype, names, line);
        }

        if (Check(TokenType.Read))
        {
            Consume();
            var names = new List<string> { Expect(TokenType.Id).Text };
            while (Match(TokenType.Comma)) names.Add(Expect(TokenType.Id).Text);
            Expect(TokenType.Semi);
            return new ReadStmt(names, line);
        }

        if (Check(TokenType.Write))
        {
            Consume();
            var exprs = new List<Expr> { ParseExpr() };
            while (Match(TokenType.Comma)) exprs.Add(ParseExpr());
            Expect(TokenType.Semi);
            return new WriteStmt(exprs, line);
        }

        if (Check(TokenType.LBrace))
        {
            Consume();
            var stmts = new List<Stmt>();
            while (!Check(TokenType.RBrace) && !Check(TokenType.Eof))
                stmts.Add(ParseStatement());
            Expect(TokenType.RBrace);
            return new BlockStmt(stmts, line);
        }

        if (Check(TokenType.If))
        {
            Consume();
            Expect(TokenType.LParen);
            var cond = ParseExpr();
            Expect(TokenType.RParen);
            var then = ParseStatement();
            Stmt? els = null;
            if (Check(TokenType.Else)) { Consume(); els = ParseStatement(); }
            return new IfStmt(cond, then, els, line);
        }

        if (Check(TokenType.While))
        {
            Consume();
            Expect(TokenType.LParen);
            var cond = ParseExpr();
            Expect(TokenType.RParen);
            var body = ParseStatement();
            return new WhileStmt(cond, body, line);
        }

        // expression statement
        var expr = ParseExpr();
        Expect(TokenType.Semi);
        return new ExprStmt(expr, line);
    }

    private bool IsType(TokenType tt) =>
        tt is TokenType.Int or TokenType.Float or TokenType.Bool or TokenType.String;

    private VarType ParseType()
    {
        var t = Consume();
        return t.Type switch
        {
            TokenType.Int    => VarType.Int,
            TokenType.Float  => VarType.Float,
            TokenType.Bool   => VarType.Bool,
            TokenType.String => VarType.String,
            _ => throw new Exception($"Line {t.Line}: expected type")
        };
    }

    // Pratt / precedence-climbing parser
    private Expr ParseExpr() => ParseAssign();

    private Expr ParseAssign()
    {
        // assignment: ID = expr (right associative)
        if (Check(TokenType.Id) && Peek().Type == TokenType.Eq)
        {
            int line = Current.Line;
            string name = Consume().Text;
            Consume(); // '='
            var val = ParseAssign();
            return new AssignExpr(name, val, line);
        }
        return ParseOr();
    }

    private Expr ParseOr()
    {
        var left = ParseAnd();
        while (Check(TokenType.OrOr))
        {
            int line = Current.Line;
            Consume();
            left = new BinopExpr("||", left, ParseAnd(), line);
        }
        return left;
    }

    private Expr ParseAnd()
    {
        var left = ParseEq();
        while (Check(TokenType.AndAnd))
        {
            int line = Current.Line;
            Consume();
            left = new BinopExpr("&&", left, ParseEq(), line);
        }
        return left;
    }

    private Expr ParseEq()
    {
        var left = ParseRel();
        while (Check(TokenType.EqEq) || Check(TokenType.NotEq))
        {
            int line = Current.Line;
            string op = Consume().Text;
            left = new BinopExpr(op, left, ParseRel(), line);
        }
        return left;
    }

    private Expr ParseRel()
    {
        var left = ParseAdd();
        while (Check(TokenType.Lt) || Check(TokenType.Gt))
        {
            int line = Current.Line;
            string op = Consume().Text;
            left = new BinopExpr(op, left, ParseAdd(), line);
        }
        return left;
    }

    private Expr ParseAdd()
    {
        var left = ParseMul();
        while (Check(TokenType.Plus) || Check(TokenType.Minus) || Check(TokenType.Dot))
        {
            int line = Current.Line;
            string op = Consume().Text;
            left = new BinopExpr(op, left, ParseMul(), line);
        }
        return left;
    }

    private Expr ParseMul()
    {
        var left = ParseUnary();
        while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
        {
            int line = Current.Line;
            string op = Consume().Text;
            left = new BinopExpr(op, left, ParseUnary(), line);
        }
        return left;
    }

    private Expr ParseUnary()
    {
        int line = Current.Line;
        if (Check(TokenType.Bang)) { Consume(); return new UnopExpr("!", ParseUnary(), line); }
        if (Check(TokenType.Minus)) { Consume(); return new UnopExpr("-", ParseUnary(), line); }
        return ParsePrimary();
    }

    private Expr ParsePrimary()
    {
        int line = Current.Line;
        if (Check(TokenType.LParen))
        {
            Consume();
            var e = ParseExpr();
            Expect(TokenType.RParen);
            return e;
        }
        if (Check(TokenType.IntLit))
        {
            var t = Consume();
            return new IntLitExpr(int.Parse(t.Text), line);
        }
        if (Check(TokenType.FloatLit))
        {
            var t = Consume();
            return new FloatLitExpr(double.Parse(t.Text, System.Globalization.CultureInfo.InvariantCulture), line);
        }
        if (Check(TokenType.BoolLit))
        {
            var t = Consume();
            return new BoolLitExpr(t.Text == "true", line);
        }
        if (Check(TokenType.StrLit))
        {
            var t = Consume();
            return new StrLitExpr(t.Text, line);
        }
        if (Check(TokenType.Id))
        {
            var t = Consume();
            Expr node = new IdExpr(t.Text, line);
            
            while (true)
            {
                if (Match(TokenType.LBracket)) // Handle arr[i]
                {
                    var index = ParseExpr();
                    Expect(TokenType.RBracket);
                    node = new IndexExpr(node, index, line);
                }
                else if (Match(TokenType.LParen)) // Handle fopen(...)
                {
                    var args = new List<Expr>();
                    if (!Check(TokenType.RParen))
                    {
                        args.Add(ParseExpr());
                        while (Match(TokenType.Comma)) args.Add(ParseExpr());
                    }
                    Expect(TokenType.RParen);
                    node = new IdExpr(t.Text, line); // legacy parser: call not supported
                }
                else break;
            }
            return node;
        }
        throw new Exception($"Line {line}: unexpected token '{Current.Text}'");
    }
}
