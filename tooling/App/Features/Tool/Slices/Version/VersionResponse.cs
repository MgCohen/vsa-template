namespace Umpire.Cli.Features.Tool.Slices.Version;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record VersionResponse(int ExitCode) : ToolResponse(ExitCode);
