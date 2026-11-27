namespace Umpire.Cli.Features.Scaffold.Slices.AddPack;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class AddPackHandler : ToolHandler<AddPackRequest, AddPackResponse> {
    protected override Task<AddPackResponse> Run(AddPackRequest request, CancellationToken ct) =>
        Task.FromResult(new AddPackResponse(AddFiles.Run(
            AddKind.Pack, request.Name, null, "")));
}
