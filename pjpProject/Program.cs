using Antlr4.Runtime;
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

// 1. ANTLR lex + parse
var inputStream  = new AntlrInputStream(src);
var antlrLexer   = new LanguageLexer(inputStream);
var tokenStream  = new CommonTokenStream(antlrLexer);
var antlrParser  = new LanguageParser(tokenStream);

var errors = new ErrorCollector();
antlrLexer.RemoveErrorListeners();
antlrLexer.AddErrorListener(errors);
antlrParser.RemoveErrorListeners();
antlrParser.AddErrorListener(errors);

var tree = antlrParser.program();

if (errors.Errors.Count > 0)
{
    foreach (var e in errors.Errors) Console.Error.WriteLine(e);
    return 1;
}

// 2. Build AST from parse tree
var stmts = new AstBuilder().BuildProgram(tree);

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
