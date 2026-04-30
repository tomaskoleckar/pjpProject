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

            case ArrayDeclStmt a:
                if (_vars.ContainsKey(a.Name))
                    _errors.Add($"Line {a.Line}: variable '{a.Name}' already declared");
                else
                    _vars[a.Name] = a.ElemType switch
                    {
                        VarType.Int    => VarType.IntArray,
                        VarType.Float  => VarType.FloatArray,
                        VarType.Bool   => VarType.BoolArray,
                        VarType.String => VarType.StringArray,
                        _ => throw new Exception($"Line {a.Line}: invalid array element type")
                    };
                break;

            case FileDeclStmt f:
                foreach (var name in f.Names)
                {
                    if (_vars.ContainsKey(name))
                        _errors.Add($"Line {f.Line}: variable '{name}' already declared");
                    else
                        _vars[name] = VarType.File;
                }
                break;

            case FopenStmt f:
                if (!_vars.TryGetValue(f.VarName, out var ft))
                    _errors.Add($"Line {f.Line}: undeclared variable '{f.VarName}'");
                else if (ft != VarType.File)
                    _errors.Add($"Line {f.Line}: '{f.VarName}' is not a file variable");
                break;

            case FileWriteStmt fw:
                if (!_vars.TryGetValue(fw.VarName, out var fwt))
                    _errors.Add($"Line {fw.Line}: undeclared variable '{fw.VarName}'");
                else if (fwt != VarType.File)
                    _errors.Add($"Line {fw.Line}: '{fw.VarName}' is not a file variable");
                foreach (var e in fw.Values) InferType(e);
                break;

            case ArrayAssignStmt a:
            {
                if (!_vars.TryGetValue(a.Name, out var at))
                {
                    _errors.Add($"Line {a.Line}: undeclared variable '{a.Name}'");
                    break;
                }
                var elem = ArrayElemType(at);
                if (elem == null) { _errors.Add($"Line {a.Line}: '{a.Name}' is not an array"); break; }
                var idxT = InferType(a.Index);
                if (idxT != null && idxT != VarType.Int)
                    _errors.Add($"Line {a.Line}: array index must be int");
                var valT = InferType(a.Value);
                if (valT != null && !Compatible(elem.Value, valT.Value, out _))
                    _errors.Add($"Line {a.Line}: cannot store {valT} in {elem}[]");
                break;
            }

            case ExprStmt e:
                InferType(e.Expr);
                break;

            case ReadStmt r:
                foreach (var name in r.Names)
                    if (!_vars.ContainsKey(name))
                        _errors.Add($"Line {r.Line}: undeclared variable '{name}'");
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
                if (rType != null && !Compatible(lType, rType.Value, out _))
                    _errors.Add($"Line {a.Line}: cannot assign {rType} to {lType}");
                return lType;
            }

            case UnopExpr u:
            {
                var t = InferType(u.Operand);
                if (t == null) return null;
                if (u.Op == "-")
                {
                    if (t != VarType.Int && t != VarType.Float)
                    { _errors.Add($"Line {u.Line}: unary '-' requires int or float"); return null; }
                    return t;
                }
                if (u.Op == "!")
                {
                    if (t != VarType.Bool)
                    { _errors.Add($"Line {u.Line}: '!' requires bool"); return null; }
                    return VarType.Bool;
                }
                return null;
            }

            case IndexExpr idx:
            {
                var targetType = InferType(idx.Target);
                var indexType  = InferType(idx.Index);
                if (targetType == null || indexType == null) return null;
                if (indexType != VarType.Int)
                    _errors.Add($"Line {idx.Line}: array index must be int");
                var elem = ArrayElemType(targetType.Value);
                if (elem == null)
                    _errors.Add($"Line {idx.Line}: expression is not an array");
                return elem;
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
                if (IsNumeric(l.Value) && IsNumeric(r.Value)) return VarType.Float;
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
                if (IsNumeric(l.Value) && IsNumeric(r.Value)) return VarType.Bool;
                _errors.Add($"Line {b.Line}: '{b.Op}' requires int or float operands");
                return null;

            case "==": case "!=":
                if ((IsNumeric(l.Value) && IsNumeric(r.Value)) || l == r)
                    return VarType.Bool;
                _errors.Add($"Line {b.Line}: '{b.Op}' operands must be same type");
                return null;

            case "&&": case "||":
                if (l == VarType.Bool && r == VarType.Bool) return VarType.Bool;
                _errors.Add($"Line {b.Line}: '{b.Op}' requires bool operands");
                return null;
        }
        return null;
    }

    public static VarType? ArrayElemType(VarType t) => t switch
    {
        VarType.IntArray    => VarType.Int,
        VarType.FloatArray  => VarType.Float,
        VarType.BoolArray   => VarType.Bool,
        VarType.StringArray => VarType.String,
        _                   => null
    };

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
