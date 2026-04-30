namespace pjpProject;

public class Interpreter
{
    private readonly Stack<object> _stack = new();
    private readonly Dictionary<string, object> _vars = new();
    private readonly List<string> _instructions = new();
    private readonly Dictionary<int, int> _labels = new(); // label number -> instruction index
    private int _ip;

    public void Run(string code, TextReader? input = null, TextWriter? output = null)
    {
        input  ??= Console.In;
        output ??= Console.Out;

        // parse instructions
        _instructions.Clear();
        foreach (var raw in code.Split('\n'))
        {
            var line = raw.Trim();
            if (!string.IsNullOrEmpty(line)) _instructions.Add(line);
        }

        // first pass: collect labels
        for (int i = 0; i < _instructions.Count; i++)
        {
            var parts = _instructions[i].Split(' ', 2);
            if (parts[0] == "label")
                _labels[int.Parse(parts[1])] = i;
        }

        _ip = 0;
        while (_ip < _instructions.Count)
        {
            Execute(_instructions[_ip], ref _ip, input, output);
            _ip++;
        }
    }

    private void Execute(string instr, ref int ip, TextReader input, TextWriter output)
    {
        var parts = Tokenize(instr);
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "push":
            {
                string t = parts[1];
                string v = parts[2];
                _stack.Push(t switch
                {
                    "I" => (object)int.Parse(v),
                    "F" => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
                    "B" => v == "true",
                    "S" => ParseString(v),
                    _   => throw new Exception($"Unknown push type {t}")
                });
                break;
            }

            case "pop": _stack.Pop(); break;

            case "load":
            {
                string name = parts[1];
                if (!_vars.TryGetValue(name, out var val))
                    throw new Exception($"Variable '{name}' not initialized");
                _stack.Push(val);
                break;
            }

            case "save":
            {
                string name = parts[1];
                _vars[name] = _stack.Pop();
                break;
            }

            case "label": break; // already processed

            case "jmp":
                ip = _labels[int.Parse(parts[1])];
                break;

            case "fjmp":
            {
                bool val = (bool)_stack.Pop();
                if (!val) ip = _labels[int.Parse(parts[1])];
                break;
            }

            case "add":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a + (int)b : (object)((double)ToFloat(a) + (double)ToFloat(b)));
                break;
            }

            case "sub":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a - (int)b : (object)((double)ToFloat(a) - (double)ToFloat(b)));
                break;
            }

            case "mul":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a * (int)b : (object)((double)ToFloat(a) * (double)ToFloat(b)));
                break;
            }

            case "div":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a / (int)b : (object)((double)ToFloat(a) / (double)ToFloat(b)));
                break;
            }

            case "mod":
            {
                var b = (int)_stack.Pop(); var a = (int)_stack.Pop();
                _stack.Push(a % b);
                break;
            }

            case "uminus":
                _stack.Push(parts[1] == "I" ? -(int)_stack.Pop() : (object)(-(double)ToFloat(_stack.Pop())));
                break;

            case "concat":
            {
                var b = (string)_stack.Pop(); var a = (string)_stack.Pop();
                _stack.Push(a + b);
                break;
            }

            case "and":
            {
                var b = (bool)_stack.Pop(); var a = (bool)_stack.Pop();
                _stack.Push(a && b);
                break;
            }

            case "or":
            {
                var b = (bool)_stack.Pop(); var a = (bool)_stack.Pop();
                _stack.Push(a || b);
                break;
            }

            case "not": _stack.Push(!(bool)_stack.Pop()); break;

            case "gt":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a > (int)b : (object)((double)ToFloat(a) > (double)ToFloat(b)));
                break;
            }

            case "lt":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] == "I" ? (int)a < (int)b : (object)((double)ToFloat(a) < (double)ToFloat(b)));
                break;
            }

            case "eq":
            {
                var b = _stack.Pop(); var a = _stack.Pop();
                _stack.Push(parts[1] switch
                {
                    "I" => (int)a == (int)b,
                    "F" => (double)ToFloat(a) == (double)ToFloat(b),
                    "S" => (string)a == (string)b,
                    _   => false
                });
                break;
            }

            case "itof": _stack.Push((double)(int)_stack.Pop()); break;

            case "print":
            {
                int n = int.Parse(parts[1]);
                var vals = new object[n];
                for (int i = n - 1; i >= 0; i--) vals[i] = _stack.Pop();
                for (int i = 0; i < n; i++) output.Write(FormatValue(vals[i]));
                output.WriteLine();
                break;
            }

            case "read":
            {
                string line = input.ReadLine() ?? "";
                object val = parts[1] switch
                {
                    "I" => int.Parse(line),
                    "F" => double.Parse(line, System.Globalization.CultureInfo.InvariantCulture),
                    "B" => line.Trim() == "true",
                    "S" => line,
                    _   => throw new Exception($"Unknown read type {parts[1]}")
                };
                _stack.Push(val);
                break;
            }

            default:
                throw new Exception($"Unknown instruction: {parts[0]}");
        }
    }

    private static double ToFloat(object o) => o is int i ? (double)i : (double)o;

    private static string FormatValue(object v) => v switch
    {
        bool b   => b ? "true" : "false",
        double d => d == Math.Floor(d) && !double.IsInfinity(d)
                        ? d.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                        : d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _        => v.ToString() ?? ""
    };

    // tokenize an instruction line, respecting quoted strings
    private static string[] Tokenize(string line)
    {
        var result = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && line[i] == ' ') i++;
            if (i >= line.Length) break;
            if (line[i] == '"')
            {
                int start = i++;
                while (i < line.Length && line[i] != '"') i++;
                i++; // closing "
                result.Add(line[start..i]);
            }
            else
            {
                int start = i;
                while (i < line.Length && line[i] != ' ') i++;
                result.Add(line[start..i]);
            }
        }
        return result.ToArray();
    }

    private static string ParseString(string raw)
    {
        // remove surrounding quotes
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            return raw[1..^1];
        return raw;
    }
}
