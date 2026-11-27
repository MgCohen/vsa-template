namespace Umpire.Cli.Features.Scaffold.Slices.AddModule;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record AddModuleResponse(int ExitCode) : ToolResponse(ExitCode);
