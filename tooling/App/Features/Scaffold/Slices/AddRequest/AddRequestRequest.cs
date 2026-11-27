namespace Umpire.Cli.Features.Scaffold.Slices.AddRequest;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record AddRequestRequest(string Name, string? Module)
    : IRequest<AddRequestResponse>, IToolRequest<AddRequestRequest> {
    public static readonly CommandOption ModuleOption = new(["-m", "--module"], "Module", Required: true);

    public static CommandForm Form { get; } = new(
        ["add", "request"],
        "Adds a request, its response and its handler.",
        6) {
        TakesName = true,
        Options = [ModuleOption],
    };

    public static string MissingModule => Form.Missing(ModuleOption);

    public static AddRequestRequest From(ToolCall call) {
        var input = call.Input(Form);
        return new(input.Name, input.Value(ModuleOption));
    }
}
