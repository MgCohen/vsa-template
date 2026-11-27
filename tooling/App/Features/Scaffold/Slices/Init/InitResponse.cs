namespace Umpire.Cli.Features.Scaffold.Slices.Init;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record InitResponse(int ExitCode) : ToolResponse(ExitCode);
