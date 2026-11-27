namespace Umpire.Cli.Features.Scaffold.Slices.AddCommand;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record AddCommandRequest(string Name, string? Module)
    : IRequest<AddCommandResponse>, IToolRequest<AddCommandRequest> {
    public static readonly CommandOption ModuleOption = new(["-m", "--module"], "Module", Required: true);

    public static CommandForm Form { get; } = new(
        ["add", "command"],
        "Adds a command, its result and its handler.",
        7) {
        TakesName = true,
        Options = [ModuleOption],
    };

    public static string MissingModule => Form.Missing(ModuleOption);

    public static AddCommandRequest From(ToolCall call) {
        var input = call.Input(Form);
        return new(input.Name, input.Value(ModuleOption));
    }
}
