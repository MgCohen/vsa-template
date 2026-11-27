namespace Umpire.Cli.Features.Versions.Slices.Doctor;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record DoctorRequest : IRequest<DoctorResponse>, IToolRequest<DoctorRequest> {
    public static CommandForm Form { get; } = new(
        ["doctor"],
        "Makes sure that the tool and the Umpire packages of the app have one version.",
        10);

    public static DoctorRequest From(ToolCall call) {
        call.Input(Form);
        return new();
    }
}
