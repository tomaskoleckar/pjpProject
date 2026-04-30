using Antlr4.Runtime;

namespace pjpProject;

public class ErrorCollector : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{
    public List<string> Errors { get; } = new();

    // lexer errors (symbol type = int)
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => Errors.Add($"Line {line}:{charPositionInLine} {msg}");

    // parser errors (symbol type = IToken)
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => Errors.Add($"Line {line}:{charPositionInLine} {msg}");
}
