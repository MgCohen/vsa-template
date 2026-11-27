namespace Umpire.Cli.Features.Scaffold.Slices.AddModule;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record AddModuleRequest(string Name)
    : IRequest<AddModuleResponse>, IToolRequest<AddModuleRequest> {
    public static CommandForm Form { get; } = new(
        ["add", "module"],
        "Adds a module with all layers and a starter page.",
        3) { TakesName = true };

    public static AddModuleRequest From(ToolCall call) => new(call.Input(Form).Name);
}
