using System.CommandLine;

namespace TypedGML.CLI.Commands;

public interface ICommand
{
    Command BuildCommand();
}