namespace Umpire.Cli.Features.Scaffold.Slices.AddPlugin;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class AddPluginHandler : ToolHandler<AddPluginRequest, AddPluginResponse> {
    protected override Task<AddPluginResponse> Run(AddPluginRequest request, CancellationToken ct) =>
        Task.FromResult(new AddPluginResponse(AddFiles.Run(
            AddKind.Plugin, request.Name, null, "")));
}
