namespace pjpProject;

public class CodeGen
{
    private readonly TypeChecker _tc;
    private readonly System.Text.StringBuilder _sb = new();
    private int _labelCounter;

    public CodeGen(TypeChecker tc) => _tc = tc;

    private int NewLabel() => _labelCounter++;

    private void Emit(string instr) => _sb.AppendLine(instr);

    public string Generate(List<Stmt> stmts)
    {
        foreach (var s in stmts) GenStmt(s);
        return _sb.ToString();
    }

    private void GenStmt(Stmt s)
    {
        switch (s)
        {
            case EmptyStmt: break;

            case DeclStmt d:
                foreach (var name in d.Names)
                {
                    switch (d.VType)
                    {
                        case VarType.Int:    Emit("push I 0");     break;
                        case VarType.Float:  Emit("push F 0.0");   break;
                        case VarType.Bool:   Emit("push B false"); break;
                        case VarType.String: Emit("push S \"\"");  break;
                        case VarType.File:   Emit("push S \"\"");  break;
                    }
                    Emit($"save {name}");
                }
                break;

            case ArrayDeclStmt a:
            {
                var tc = TypeCode(a.ElemType);
                Emit($"anew {a.Name} {a.Size} {tc}");
                break;
            }

            case FileDeclStmt f:
                foreach (var name in f.Names)
                {
                    Emit("push S \"\"");
                    Emit($"save {name}");
                }
                break;

            case FopenStmt f:
                Emit($"fopen {f.VarName} \"{f.FileName}\"");
                break;

            case FileWriteStmt fw:
                foreach (var v in fw.Values) GenExpr(v);
                Emit($"fwrite {fw.VarName} {fw.Values.Count}");
                break;

            case ArrayAssignStmt a:
            {
                GenExpr(a.Index);
                GenExpr(a.Value);
                var arrElem = TypeChecker.ArrayElemType(_tc.Variables[a.Name])!.Value;
                var valType = _tc.InferType(a.Value);
                if (valType == VarType.Int && arrElem == VarType.Float) Emit("itof");
                Emit($"astore {a.Name}");
                break;
            }

            case ExprStmt e:
                GenExpr(e.Expr);
                Emit("pop");
                break;

            case ReadStmt r:
                foreach (var name in r.Names)
                {
                    var vt = _tc.Variables[name];
                    Emit($"read {TypeCode(vt)}");
                    Emit($"save {name}");
                }
                break;

            case WriteStmt w:
                foreach (var e in w.Exprs) GenExpr(e);
                Emit($"print {w.Exprs.Count}");
                break;

            case BlockStmt b:
                foreach (var st in b.Stmts) GenStmt(st);
                break;

            case IfStmt i:
            {
                int elseLabel = NewLabel();
                int endLabel  = NewLabel();
                GenExpr(i.Cond);
                Emit($"fjmp {elseLabel}");
                GenStmt(i.Then);
                Emit($"jmp {endLabel}");
                Emit($"label {elseLabel}");
                if (i.Else != null) GenStmt(i.Else);
                Emit($"label {endLabel}");
                break;
            }

            case WhileStmt w:
            {
                int startLabel = NewLabel();
                int endLabel   = NewLabel();
                Emit($"label {startLabel}");
                GenExpr(w.Cond);
                Emit($"fjmp {endLabel}");
                GenStmt(w.Body);
                Emit($"jmp {startLabel}");
                Emit($"label {endLabel}");
                break;
            }
        }
    }

    private void GenExpr(Expr e)
    {
        switch (e)
        {
            case IntLitExpr i:   Emit($"push I {i.Value}"); break;
            case FloatLitExpr f: Emit($"push F {f.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); break;
            case BoolLitExpr b:  Emit($"push B {(b.Value ? "true" : "false")}"); break;
            case StrLitExpr s:   Emit($"push S \"{s.Value}\""); break;

            case IdExpr id:      Emit($"load {id.Name}"); break;

            case IndexExpr idx:
            {
                var arrName = ((IdExpr)idx.Target).Name;
                GenExpr(idx.Index);
                Emit($"aload {arrName}");
                break;
            }

            case AssignExpr a:
            {
                GenExpr(a.Value);
                var lType = _tc.Variables[a.Name];
                var rType = _tc.InferType(a.Value);
                if (rType == VarType.Int && lType == VarType.Float) Emit("itof");
                Emit($"save {a.Name}");
                Emit($"load {a.Name}");
                break;
            }

            case UnopExpr u:
                GenExpr(u.Operand);
                switch (u.Op)
                {
                    case "-":
                        var ut = _tc.InferType(u.Operand);
                        Emit($"uminus {TypeCode(ut!.Value)}");
                        break;
                    case "!": Emit("not"); break;
                }
                break;

            case BinopExpr b:
                GenBinop(b);
                break;
        }
    }

    private void GenBinop(BinopExpr b)
    {
        var lt = _tc.InferType(b.Left)!.Value;
        var rt = _tc.InferType(b.Right)!.Value;

        // determine result type for arithmetic/relational
        bool needCastL = false, needCastR = false;
        VarType arithType = lt;
        if (b.Op is "+" or "-" or "*" or "/" or "<" or ">" or "==" or "!=")
        {
            if (lt == VarType.Float || rt == VarType.Float)
            {
                arithType = VarType.Float;
                if (lt == VarType.Int) needCastL = true;
                if (rt == VarType.Int) needCastR = true;
            }
        }

        GenExpr(b.Left);
        if (needCastL) Emit("itof");
        GenExpr(b.Right);
        if (needCastR) Emit("itof");

        string tc = TypeCode(arithType);

        switch (b.Op)
        {
            case "+":  Emit($"add {tc}"); break;
            case "-":  Emit($"sub {tc}"); break;
            case "*":  Emit($"mul {tc}"); break;
            case "/":  Emit($"div {tc}"); break;
            case "%":  Emit("mod"); break;
            case ".":  Emit("concat"); break;
            case "<":  Emit($"lt {tc}"); break;
            case ">":  Emit($"gt {tc}"); break;
            case "==": Emit($"eq {tc}"); break;
            case "!=": Emit($"eq {tc}"); Emit("not"); break;
            case "&&": Emit("and"); break;
            case "||": Emit("or"); break;
        }
    }

    private static string TypeCode(VarType t) => t switch
    {
        VarType.Int    => "I",
        VarType.Float  => "F",
        VarType.Bool   => "B",
        VarType.String => "S",
        _ => "I"
    };
}
