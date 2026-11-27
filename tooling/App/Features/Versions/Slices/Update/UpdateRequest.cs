namespace Umpire.Cli.Features.Versions.Slices.Update;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record UpdateRequest(string? Version) : IRequest<UpdateResponse>, IToolRequest<UpdateRequest> {
    public static readonly CommandOption VersionOption = new(["--version"], "version");

    public static CommandForm Form { get; } = new(
        ["update"],
        "Moves the Umpire packages of the app to the version of this tool, or to <version>.",
        11) { Options = [VersionOption] };

    public static UpdateRequest From(ToolCall call) => new(call.Input(Form).Value(VersionOption));
}
