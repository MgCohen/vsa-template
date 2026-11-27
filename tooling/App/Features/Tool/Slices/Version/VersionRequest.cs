namespace Umpire.Cli.Features.Tool.Slices.Version;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

sealed record VersionRequest : IRequest<VersionResponse>, IToolRequest<VersionRequest> {
    public static CommandForm Form { get; } =
        new(["version"], "Shows the version of the tool.", 12) { ExactLine = ["--version"] };

    public static VersionRequest From(ToolCall call) => new();
}
