using Antlr4.Runtime;

namespace pjpProject;

public class ErrorCollector : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{
    public List<string> Errors { get; } = new();

    public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer,
        int offendingSymbol, int line, int charPositionInLine, string msg,
        RecognitionException e)
        => Errors.Add($"Line {line}:{charPositionInLine} {msg}");

    public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer,
        IToken offendingSymbol, int line, int charPositionInLine, string msg,
        RecognitionException e)
        => Errors.Add($"Line {line}:{charPositionInLine} {msg}");
}
