using pjpProject;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: pjpProject <source.pjp> [--emit <output.code>] [--run <code.file>]");
    return 1;
}

// --run mode: interpret a pre-generated code file
if (args[0] == "--run")
{
    if (args.Length < 2) { Console.Error.WriteLine("--run requires a file path"); return 1; }
    string code = File.ReadAllText(args[1]);
    new Interpreter().Run(code);
    return 0;
}

// compile mode
string src = File.ReadAllText(args[0]);

// 1. Lex
var lexer = new Lexer(src);
List<Token> tokens;
try { tokens = lexer.Tokenize(); }
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// 2. Parse
var parser = new Parser(tokens);
var stmts = parser.Parse();
if (parser.Errors.Count > 0)
{
    foreach (var e in parser.Errors) Console.Error.WriteLine(e);
    return 1;
}

// 3. Type check
var tc = new TypeChecker();
tc.Check(stmts);
if (tc.Errors.Count > 0)
{
    foreach (var e in tc.Errors) Console.Error.WriteLine(e);
    return 1;
}

// 4. Code generation
var codeGen = new CodeGen(tc);
string generated = codeGen.Generate(stmts);

// determine output path
string outPath = args.Length >= 3 && args[1] == "--emit" ? args[2]
    : Path.ChangeExtension(args[0], ".code");

File.WriteAllText(outPath, generated);
Console.WriteLine($"Code written to {outPath}");

// optionally run immediately
if (args.Contains("--run-after"))
    new Interpreter().Run(generated);

return 0;
