using TypedGML.CLI;

var exitCode = args.FirstOrDefault() switch
{
    "init" => InitCommand.Run(args.Skip(1).ToArray()),
    "build" => await BuildCommand.RunAsync(args.Skip(1).ToArray()),
    _ => UsagePrinter.Print()
};

return exitCode;
