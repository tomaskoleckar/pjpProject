namespace pjpProject;

public enum TokenType
{
    // types
    Int, Float, Bool, String,
    // keywords
    Read, Write, If, Else, While,
    // literals
    IntLit, FloatLit, BoolLit, StrLit,
    // identifier
    Id,
    // operators
    Plus, Minus, Star, Slash, Percent, Dot,
    Lt, Gt, EqEq, NotEq,
    AndAnd, OrOr, Bang,
    Eq,
    // punctuation
    LParen, RParen, LBrace, RBrace, Comma, Semi,
    Eof
}

public record Token(TokenType Type, string Text, int Line);

public class Lexer
{
    private readonly string _src;
    private int _pos;
    private int _line = 1;

    public Lexer(string src) => _src = src;

    private char Current => _pos < _src.Length ? _src[_pos] : '\0';
    private char Peek(int offset = 1) => (_pos + offset) < _src.Length ? _src[_pos + offset] : '\0';

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _src.Length)
            {
                tokens.Add(new Token(TokenType.Eof, "", _line));
                break;
            }
            tokens.Add(NextToken());
        }
        return tokens;
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _src.Length)
        {
            if (Current == '\n') { _line++; _pos++; }
            else if (Current is ' ' or '\t' or '\r') _pos++;
            else if (Current == '/' && Peek() == '/')
            {
                while (_pos < _src.Length && Current != '\n') _pos++;
            }
            else break;
        }
    }

    private Token NextToken()
    {
        int line = _line;
        char c = Current;

        // string literal
        if (c == '"')
        {
            _pos++;
            var sb = new System.Text.StringBuilder();
            while (_pos < _src.Length && Current != '"')
            {
                if (Current == '\\' && Peek() == '"') { sb.Append('"'); _pos += 2; }
                else sb.Append(Current == '\n' ? (char)(_line++, '\n').Item2 : Current);
                if (Current != '"') _pos++;
            }
            if (_pos < _src.Length) _pos++; // closing "
            return new Token(TokenType.StrLit, sb.ToString(), line);
        }

        // number
        if (char.IsDigit(c))
        {
            int start = _pos;
            while (_pos < _src.Length && char.IsDigit(Current)) _pos++;
            if (_pos < _src.Length && Current == '.' && char.IsDigit(Peek()))
            {
                _pos++; // dot
                while (_pos < _src.Length && char.IsDigit(Current)) _pos++;
                return new Token(TokenType.FloatLit, _src[start.._pos], line);
            }
            return new Token(TokenType.IntLit, _src[start.._pos], line);
        }

        // float starting with dot
        if (c == '.' && char.IsDigit(Peek()))
        {
            int start = _pos++;
            while (_pos < _src.Length && char.IsDigit(Current)) _pos++;
            return new Token(TokenType.FloatLit, _src[start.._pos], line);
        }

        // identifier or keyword
        if (char.IsLetter(c))
        {
            int start = _pos;
            while (_pos < _src.Length && (char.IsLetterOrDigit(Current))) _pos++;
            string word = _src[start.._pos];
            TokenType tt = word switch
            {
                "int"   => TokenType.Int,
                "float" => TokenType.Float,
                "bool"  => TokenType.Bool,
                "string"=> TokenType.String,
                "read"  => TokenType.Read,
                "write" => TokenType.Write,
                "if"    => TokenType.If,
                "else"  => TokenType.Else,
                "while" => TokenType.While,
                "true"  => TokenType.BoolLit,
                "false" => TokenType.BoolLit,
                _       => TokenType.Id
            };
            return new Token(tt, word, line);
        }

        _pos++;
        return c switch
        {
            '+' => new Token(TokenType.Plus,   "+", line),
            '-' => new Token(TokenType.Minus,  "-", line),
            '*' => new Token(TokenType.Star,   "*", line),
            '/' => new Token(TokenType.Slash,  "/", line),
            '%' => new Token(TokenType.Percent,"%", line),
            '.' => new Token(TokenType.Dot,    ".", line),
            '<' => new Token(TokenType.Lt,     "<", line),
            '>' => new Token(TokenType.Gt,     ">", line),
            '=' when Current == '=' => Advance(TokenType.EqEq,  "==", line),
            '!' when Current == '=' => Advance(TokenType.NotEq, "!=", line),
            '&' when Current == '&' => Advance(TokenType.AndAnd,"&&", line),
            '|' when Current == '|' => Advance(TokenType.OrOr,  "||", line),
            '=' => new Token(TokenType.Eq,     "=", line),
            '!' => new Token(TokenType.Bang,   "!", line),
            '(' => new Token(TokenType.LParen, "(", line),
            ')' => new Token(TokenType.RParen, ")", line),
            '{' => new Token(TokenType.LBrace, "{", line),
            '}' => new Token(TokenType.RBrace, "}", line),
            ',' => new Token(TokenType.Comma,  ",", line),
            ';' => new Token(TokenType.Semi,   ";", line),
            _   => throw new Exception($"Unknown character '{c}' at line {line}")
        };
    }

    private Token Advance(TokenType tt, string text, int line) { _pos++; return new Token(tt, text, line); }
}
