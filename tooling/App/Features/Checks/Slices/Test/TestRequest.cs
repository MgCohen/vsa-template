namespace Umpire.Cli.Features.Checks.Slices.Test;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record TestRequest(string? Configuration)
    : IRequest<TestResponse>, IToolRequest<TestRequest>, IChildProcessRequest {
    public static readonly CommandOption ConfigurationOption = new(["-c", "--configuration"], "configuration");

    public static CommandForm Form { get; } = new(
        ["test"],
        "Runs the tests of each project in the app whose name ends with .Tests. It needs no global.json.",
        8) { Options = [ConfigurationOption] };

    public static TestRequest From(ToolCall call) => new(call.Input(Form).Value(ConfigurationOption));
}
