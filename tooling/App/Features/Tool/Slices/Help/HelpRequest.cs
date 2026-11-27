namespace Umpire.Cli.Features.Tool.Slices.Help;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record HelpRequest(IReadOnlyList<string> Args, IReadOnlyList<CommandForm> Commands)
    : IRequest<HelpResponse>, IToolRequest<HelpRequest> {
    public static CommandForm Form { get; } = new(["help"], "Shows the usage of the tool.", 0) { Listed = false };

    public static HelpRequest From(ToolCall call) => new(call.Args, call.Commands);
}
