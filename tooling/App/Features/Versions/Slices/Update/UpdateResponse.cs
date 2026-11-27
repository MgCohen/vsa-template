namespace Umpire.Cli.Features.Versions.Slices.Update;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record UpdateResponse(int ExitCode) : ToolResponse(ExitCode);
