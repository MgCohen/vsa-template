namespace Umpire.Cli.Features.Scaffold.Slices.AddCommand;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class AddCommandHandler : ToolHandler<AddCommandRequest, AddCommandResponse> {
    protected override Task<AddCommandResponse> Run(AddCommandRequest request, CancellationToken ct) =>
        Task.FromResult(new AddCommandResponse(AddFiles.Run(
            AddKind.Command, request.Name, request.Module, AddCommandRequest.MissingModule)));
}
