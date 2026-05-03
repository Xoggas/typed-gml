namespace TypedGML.CLI;

internal sealed class CliOptions
{
    private CliOptions(string inputPath, string? outputPath, string? gmProjectPath, string bclPath)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        GmProjectPath = gmProjectPath;
        BclPath = bclPath;
    }

    public string InputPath { get; }

    public string? OutputPath { get; }

    public string? GmProjectPath { get; }

    public string BclPath { get; }

    public static CliOptions Parse(string[] args)
    {
        var positional = new List<string>();
        string? outputPath = null;
        string? gmProjectPath = null;
        string? bclPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--output")
                outputPath = ReadValue(args, ref i, arg);
            else if (arg == "--gm-project")
                gmProjectPath = ReadValue(args, ref i, arg);
            else if (arg is "--bcl" or "--bcl-path")
                bclPath = ReadValue(args, ref i, arg);
            else if (arg.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unknown option '{arg}'.", nameof(args));
            else
                positional.Add(arg);
        }

        if (positional.Count == 0)
            throw new ArgumentException(Usage(), nameof(args));

        if (positional.Count > 3)
            throw new ArgumentException(Usage(), nameof(args));

        outputPath ??= positional.ElementAtOrDefault(1);
        bclPath ??= positional.ElementAtOrDefault(2) ?? Path.Combine(AppContext.BaseDirectory, "bcl");

        if (outputPath is null && gmProjectPath is null)
            throw new ArgumentException(Usage(), nameof(args));

        return new CliOptions(positional[0], outputPath, gmProjectPath, bclPath);
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Missing value for {optionName}.", nameof(args));

        index++;
        return args[index];
    }

    private static string Usage() =>
        "Usage: TypedGML.CLI <inputPath> [outputPath] [bclPath] [--output <path>] [--gm-project <path>]";
}
