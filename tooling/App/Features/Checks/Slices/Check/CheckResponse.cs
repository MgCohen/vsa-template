namespace Umpire.Cli.Features.Checks.Slices.Check;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record CheckResponse(int ExitCode) : ToolResponse(ExitCode);
