namespace Umpire.Cli.Features.Checks.Slices.Test;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record TestResponse(int ExitCode) : ToolResponse(ExitCode);
