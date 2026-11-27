namespace Umpire.Cli.Features.Scaffold.Slices.AddCommand;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record AddCommandResponse(int ExitCode) : ToolResponse(ExitCode);
