using System.CommandLine;
using TypedGML.CLI.Commands;

var rootCommand = new RootCommand
{
    Description = "TGML CLI"
};

rootCommand.Add(new InitCommand().BuildCommand());
rootCommand.Add(new BuildProjectCommand().BuildCommand());

ParseResult parseResult = rootCommand.Parse(args);

return parseResult.Invoke();