using System.IO;
using Antlr4.Runtime;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Parsing;

public sealed class ParseErrorListener(DiagnosticBag diagnostics, string filePath) : BaseErrorListener, IAntlrErrorListener<int>
{
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e) =>
        Report(line, charPositionInLine, msg);

    void IAntlrErrorListener<int>.SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e) =>
        Report(line, charPositionInLine, msg);

    private void Report(int line, int charPositionInLine, string msg) =>
        diagnostics.Report(
            DiagnosticCode.ParseError,
            DiagnosticSeverity.Error,
            msg,
            new SourceLocation(filePath, line, charPositionInLine));
}
