namespace Umpire.Cli.Features.Versions.Slices.Doctor;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;

sealed record DoctorResponse(int ExitCode) : ToolResponse(ExitCode);
