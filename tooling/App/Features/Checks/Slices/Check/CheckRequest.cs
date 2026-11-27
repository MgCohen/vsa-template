namespace Umpire.Cli.Features.Checks.Slices.Check;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record CheckRequest(string? Configuration, string? Mode)
    : IRequest<CheckResponse>, IToolRequest<CheckRequest>, IChildProcessRequest {
    public static readonly CommandOption ConfigurationOption = new(["-c", "--configuration"], "Debug|Release");
    public static readonly CommandOption ModeOption = new(["--mode"], "Warn|Error");

    public static CommandForm Form { get; } = new(
        ["check"],
        "Builds the app and checks it with the rule engine of the app (the version of its Umpire.Engine package).",
        9) { Options = [ConfigurationOption, ModeOption] };

    public static CheckRequest From(ToolCall call) {
        var input = call.Input(Form);
        return new(input.Value(ConfigurationOption), input.Value(ModeOption));
    }
}
