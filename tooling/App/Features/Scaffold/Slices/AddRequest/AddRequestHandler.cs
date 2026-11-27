namespace Umpire.Cli.Features.Scaffold.Slices.AddRequest;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class AddRequestHandler : ToolHandler<AddRequestRequest, AddRequestResponse> {
    protected override Task<AddRequestResponse> Run(AddRequestRequest request, CancellationToken ct) =>
        Task.FromResult(new AddRequestResponse(AddFiles.Run(
            AddKind.Request, request.Name, request.Module, AddRequestRequest.MissingModule)));
}
