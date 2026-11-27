namespace Umpire.Cli.Features.Scaffold.Slices.AddPack;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record AddPackRequest(string Name)
    : IRequest<AddPackResponse>, IToolRequest<AddPackRequest> {
    public static CommandForm Form { get; } = new(
        ["add", "pack"],
        "Adds a pack variant: a kernel that inherits VsaKernel, and an architecture for that kernel.",
        5) { TakesName = true };

    public static AddPackRequest From(ToolCall call) => new(call.Input(Form).Name);
}
