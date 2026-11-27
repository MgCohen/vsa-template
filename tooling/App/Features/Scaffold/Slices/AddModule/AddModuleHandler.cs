namespace Umpire.Cli.Features.Scaffold.Slices.AddModule;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class AddModuleHandler : ToolHandler<AddModuleRequest, AddModuleResponse> {
    protected override Task<AddModuleResponse> Run(AddModuleRequest request, CancellationToken ct) =>
        Task.FromResult(new AddModuleResponse(AddFiles.Run(
            AddKind.Module, request.Name, null, "")));
}
