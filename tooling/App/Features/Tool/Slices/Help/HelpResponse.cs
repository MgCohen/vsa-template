namespace Umpire.Cli.Features.Tool.Slices.Help;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record HelpResponse(int ExitCode) : ToolResponse(ExitCode);
