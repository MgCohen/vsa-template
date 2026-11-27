namespace Umpire.Cli.Features.Scaffold.Slices.AddPlugin;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record AddPluginRequest(string Name)
    : IRequest<AddPluginResponse>, IToolRequest<AddPluginRequest> {
    public static CommandForm Form { get; } = new(
        ["add", "plugin"],
        "Adds a plugin module: a base of the same layers in each module.",
        4) { TakesName = true };

    public static AddPluginRequest From(ToolCall call) => new(call.Input(Form).Name);
}
