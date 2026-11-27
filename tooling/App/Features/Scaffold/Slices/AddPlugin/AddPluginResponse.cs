namespace Umpire.Cli.Features.Scaffold.Slices.AddPlugin;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record AddPluginResponse(int ExitCode) : ToolResponse(ExitCode);
