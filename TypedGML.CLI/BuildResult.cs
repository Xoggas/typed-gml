using TypedGML.Compiler.Diagnostics;

namespace TypedGML.CLI;

internal sealed record BuildResult(
    DiagnosticBag Diagnostics,
    CliCompileResult? CompileResult);
