namespace Umpire.Cli.Features.Scaffold.Slices.AddPack;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record AddPackResponse(int ExitCode) : ToolResponse(ExitCode);
