namespace Umpire.Cli.Features.Scaffold.Slices.NewApp;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record NewAppResponse(int ExitCode) : ToolResponse(ExitCode);
