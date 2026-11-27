namespace Umpire.Cli.Features.Tool.Slices.Version;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class VersionHandler : ToolHandler<VersionRequest, VersionResponse> {
    protected override Task<VersionResponse> Run(VersionRequest request, CancellationToken ct) {
        Console.WriteLine(ToolVersion.Current);
        return Task.FromResult(new VersionResponse(0));
    }
}
