using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation;
using TypedGML.Transpiler.Generation.Decorators;
using TypedGML.Transpiler.Population;

namespace TypedGML.Transpiler;

/// <summary>
///     Main public entry point for the TypedGML transpiler.
///     Runs the three-stage pipeline: Population → Checking → Generation.
///     Usage:
///     var sources = new[] { new TgmlSourceFile("Player.tgml", fileContent) };
///     var result  = TranspilerApi.Transpile(sources);
///     if (result.Success)
///     foreach (var file in result.Files) /* write file.Path + file.Content */
/// </summary>
public static class TranspilerApi
{
    /// <summary>
    ///     Transpiles a set of .tgml source files into GML.
    /// </summary>
    /// <param name="sources">The source files to compile together.</param>
    /// <param name="pipeline">Optional custom check pipeline (defaults to <see cref="CheckPipeline.Default" />).</param>
    /// <param name="decoratorRegistry">
    ///     Optional custom decorator registry (defaults to
    ///     <see cref="DecoratorRegistry.Default" />).
    /// </param>
    public static TranspileResult Transpile(
        IReadOnlyList<TgmlSourceFile> sources,
        CheckPipeline? pipeline = null,
        DecoratorRegistry? decoratorRegistry = null)    {
        // ── Stage 1: Population ───────────────────────────────────────────────
        var (files, parseDiags) = Populator.Populate(sources);


        var ctx = new TranspileContext();
        foreach (var d in parseDiags)
        {
            ctx.AddError(d.Message, d.File, d.Line, d.Column);
        }

        if (ctx.HasErrors)
        {
            return new TranspileResult
            {
                Files = Array.Empty<GeneratedFile>(),
                Diagnostics = ctx.Diagnostics
            };
        }

        // ── Stage 2: Checking ────────────────────────────────────────────────
        (pipeline ?? CheckPipeline.Default).Run(ctx, files);

        if (ctx.HasErrors)
        {
            return new TranspileResult
            {
                Files = Array.Empty<GeneratedFile>(),
                Diagnostics = ctx.Diagnostics
            };
        }

        // ── Stage 3: Generation ──────────────────────────────────────────────
        var generator = new Generator(decoratorRegistry);
        var generatedFiles = generator.Generate(ctx, files);

        return new TranspileResult
        {
            Files = generatedFiles,
            Diagnostics = ctx.Diagnostics
        };
    }
}