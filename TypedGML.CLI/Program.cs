using System.CommandLine;

var rootCommand = new RootCommand
{
    Description = "TGML CLI"
};

ParseResult parseResult = rootCommand.Parse(args);

return parseResult.Invoke();