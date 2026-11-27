namespace Umpire.Cli.Features.Scaffold.Slices.Init;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record InitRequest(IReadOnlyList<string> Sources, string? Check)
    : IRequest<InitResponse>, IToolRequest<InitRequest> {
    public static readonly CommandOption Source = new(["--source"], "feed", Many: true);
    public static readonly CommandOption CheckOption = new(["--check"], "true");

    public static CommandForm Form { get; } = new(
        ["init"],
        "Adds the missing Umpire setup to the app in this folder. "
            + "With --check true, it shows the setup and writes nothing.",
        2) { Options = [Source, CheckOption] };

    public static InitRequest From(ToolCall call) {
        var input = call.Input(Form);
        return new(input.All(Source), input.Value(CheckOption));
    }
}
