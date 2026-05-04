namespace TypedGML.CLI;

internal sealed class BuildRunner(TgmlConfig config, string bclPath)
{
    public int Run()
    {
        var result = new CliCompiler(bclPath).Compile(config);
        DiagnosticPrinter.Print(result.Diagnostics, config, bclPath);

        if (result.Diagnostics.HasErrors || result.CompileResult is null)
            return PrintFailure(result.Diagnostics.Errors.Count);

        new GameMakerProjectWriter().Write(result.CompileResult, config.YypPath, config.TgmlRoot);
        PrintSuccess(result);
        return 0;
    }

    private static int PrintFailure(int errorCount)
    {
        Console.Error.WriteLine($"Build failed. {errorCount} error(s).");
        Console.Error.WriteLine("Exit code: 1");
        return 1;
    }

    private void PrintSuccess(BuildResult result)
    {
        var compileResult = result.CompileResult!;
        var yypName = Path.GetFileName(config.YypPath);
        var prefix = result.Diagnostics.Warnings.Count == 0
            ? "Build succeeded."
            : "Build succeeded with warnings.";

        Console.WriteLine($"{prefix} {compileResult.Scripts.Count} scripts, {compileResult.Objects.Count} objects \u2192 {yypName}");
    }
}
