namespace Umpire.Cli.Features.Scaffold.Slices.NewApp;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record NewAppRequest(string Name, IReadOnlyList<string> Sources)
    : IRequest<NewAppResponse>, IToolRequest<NewAppRequest> {
    public static readonly CommandOption Source = new(["--source"], "feed", Many: true);

    public static CommandForm Form { get; } = new(
        ["new", "app"],
        "Makes a new app in the folder <Name>. Each --source replaces the GitHub feed of the Umpire packages.",
        1) {
        TakesName = true,
        Options = [Source],
    };

    public static NewAppRequest From(ToolCall call) {
        var input = call.Input(Form);
        return new(input.Name, input.All(Source));
    }
}
