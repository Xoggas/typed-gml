using Antlr4.Runtime;

namespace TypedGML.Transpiler.Population;

/// <summary>ANTLR4 parser error listener that converts syntax errors into <see cref="TranspileDiagnostic"/> entries.</summary>
internal sealed class ParserErrorListener : BaseErrorListener
{
    private readonly string _fileName;
    private readonly List<TranspileDiagnostic> _diagnostics;

    public ParserErrorListener(string fileName, List<TranspileDiagnostic> diagnostics)
    {
        _fileName    = fileName;
        _diagnostics = diagnostics;
    }

    /// <inheritdoc/>
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken? offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _diagnostics.Add(new TranspileDiagnostic(
            DiagnosticSeverity.Error,
            $"Syntax error: {msg}",
            _fileName, line, charPositionInLine));
    }
}

/// <summary>ANTLR4 lexer error listener that converts tokenisation errors into <see cref="TranspileDiagnostic"/> entries.</summary>
internal sealed class LexerErrorListener : IAntlrErrorListener<int>
{
    private readonly string _fileName;
    private readonly List<TranspileDiagnostic> _diagnostics;

    public LexerErrorListener(string fileName, List<TranspileDiagnostic> diagnostics)
    {
        _fileName    = fileName;
        _diagnostics = diagnostics;
    }

    /// <inheritdoc/>
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _diagnostics.Add(new TranspileDiagnostic(
            DiagnosticSeverity.Error,
            $"Lexer error: {msg}",
            _fileName, line, charPositionInLine));
    }
}

