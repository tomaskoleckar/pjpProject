namespace pjpProject;

public class TypeChecker
{
    private readonly Dictionary<string, VarType> _vars = new();
    private readonly List<string> _errors = new();

    public List<string> Errors => _errors;

    public void Check(List<Stmt> stmts)
    {
        foreach (var s in stmts) CheckStmt(s);
    }

    private void CheckStmt(Stmt s)
    {
        switch (s)
        {
            case EmptyStmt: break;

            case DeclStmt d:
                foreach (var name in d.Names)
                {
                    if (_vars.ContainsKey(name))
                        _errors.Add($"Line {d.Line}: variable '{name}' already declared");
                    else
                        _vars[name] = d.VType;
                }
                break;

            case ExprStmt e:
                InferType(e.Expr);
                break;

            case ReadStmt r:
                foreach (var name in r.Names)
                {
                    if (!_vars.ContainsKey(name))
                        _errors.Add($"Line {r.Line}: undeclared variable '{name}'");
                }
                break;

            case WriteStmt w:
                foreach (var e in w.Exprs) InferType(e);
                break;

            case BlockStmt b:
                foreach (var st in b.Stmts) CheckStmt(st);
                break;

            case IfStmt i:
            {
                var ct = InferType(i.Cond);
                if (ct != null && ct != VarType.Bool)
                    _errors.Add($"Line {i.Line}: if condition must be bool");
                CheckStmt(i.Then);
                if (i.Else != null) CheckStmt(i.Else);
                break;
            }

            case WhileStmt w:
            {
                var ct = InferType(w.Cond);
                if (ct != null && ct != VarType.Bool)
                    _errors.Add($"Line {w.Line}: while condition must be bool");
                CheckStmt(w.Body);
                break;
            }
        }
    }

    public VarType? InferType(Expr e)
    {
        switch (e)
        {
            case IntLitExpr:   return VarType.Int;
            case FloatLitExpr: return VarType.Float;
            case BoolLitExpr:  return VarType.Bool;
            case StrLitExpr:   return VarType.String;

            case IdExpr id:
                if (!_vars.TryGetValue(id.Name, out var vt))
                {
                    _errors.Add($"Line {id.Line}: undeclared variable '{id.Name}'");
                    return null;
                }
                return vt;

            case AssignExpr a:
            {
                if (!_vars.TryGetValue(a.Name, out var lType))
                {
                    _errors.Add($"Line {a.Line}: undeclared variable '{a.Name}'");
                    return null;
                }
                var rType = InferType(a.Value);
                if (rType == null) return lType;
                if (!Compatible(lType, rType!.Value, out _))
                    _errors.Add($"Line {a.Line}: cannot assign {rType} to {lType}");
                return lType;
            }

            case UnopExpr u:
            {
                var t = InferType(u.Operand);
                if (t == null) return null;
                switch (u.Op)
                {
                    case "-":
                        if (t != VarType.Int && t != VarType.Float)
                        { _errors.Add($"Line {u.Line}: unary '-' requires int or float"); return null; }
                        return t;
                    case "!":
                        if (t != VarType.Bool)
                        { _errors.Add($"Line {u.Line}: '!' requires bool"); return null; }
                        return VarType.Bool;
                }
                return null;
            }

            case BinopExpr b:
                return CheckBinop(b);

            default:
                return null;
        }
    }

    private VarType? CheckBinop(BinopExpr b)
    {
        var l = InferType(b.Left);
        var r = InferType(b.Right);
        if (l == null || r == null) return null;

        switch (b.Op)
        {
            case "+": case "-": case "*": case "/":
                if (l == VarType.Int && r == VarType.Int) return VarType.Int;
                if (IsNumeric(l!.Value) && IsNumeric(r!.Value)) return VarType.Float;
                _errors.Add($"Line {b.Line}: operator '{b.Op}' requires int or float operands");
                return null;

            case "%":
                if (l == VarType.Int && r == VarType.Int) return VarType.Int;
                _errors.Add($"Line {b.Line}: '%' requires int operands");
                return null;

            case ".":
                if (l == VarType.String && r == VarType.String) return VarType.String;
                _errors.Add($"Line {b.Line}: '.' requires string operands");
                return null;

            case "<": case ">":
                if (IsNumeric(l!.Value) && IsNumeric(r!.Value)) return VarType.Bool;
                _errors.Add($"Line {b.Line}: '{b.Op}' requires int or float operands");
                return null;

            case "==": case "!=":
                if ((IsNumeric(l!.Value) && IsNumeric(r!.Value)) || (l == r && l != VarType.Bool))
                    return VarType.Bool;
                if (l == r) return VarType.Bool; // bool == bool allowed? spec says x in {I,F,S}
                _errors.Add($"Line {b.Line}: '{b.Op}' operands must be same type (int/float/string)");
                return null;

            case "&&": case "||":
                if (l == VarType.Bool && r == VarType.Bool) return VarType.Bool;
                _errors.Add($"Line {b.Line}: '{b.Op}' requires bool operands");
                return null;
        }
        return null;
    }

    private static bool IsNumeric(VarType t) => t is VarType.Int or VarType.Float;

    private static bool Compatible(VarType target, VarType source, out bool needsCast)
    {
        needsCast = false;
        if (target == source) return true;
        if (target == VarType.Float && source == VarType.Int) { needsCast = true; return true; }
        return false;
    }

    public Dictionary<string, VarType> Variables => _vars;
}
