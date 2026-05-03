using System;
using System.IO;

namespace TypedGML.Compiler;

public sealed class CompilerOptions
{
    private CompilerOptions(string inputPath, string outputPath, string bclPath)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        BclPath = bclPath;
    }

    public string InputPath { get; }

    public string OutputPath { get; }

    public string BclPath { get; }

    public static CompilerOptions Parse(string[] args)
    {
        if (args.Length < 2)
        {
            throw new ArgumentException(
                "Usage: TypedGML.Compiler <inputPath> <outputPath> [bclPath]",
                nameof(args));
        }

        var inputPath = args[0];
        var outputPath = args[1];
        var bclPath = args.Length > 2
            ? args[2]
            : Path.Combine(AppContext.BaseDirectory, "bcl");

        return new CompilerOptions(inputPath, outputPath, bclPath);
    }
}
