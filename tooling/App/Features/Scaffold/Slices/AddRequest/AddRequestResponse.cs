namespace Umpire.Cli.Features.Scaffold.Slices.AddRequest;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record AddRequestResponse(int ExitCode) : ToolResponse(ExitCode);
