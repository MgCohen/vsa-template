namespace Model.Slices.ExportModel;

using Umpire.CheckModel.Export;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class ExportModelHandler : CommandHandler<ExportModelCommand, ExportModelResult> {
    public override Task<Result<ExportModelResult>> HandleAsync(IEnvelope<ExportModelCommand> envelope) =>
        Task.FromResult<Result<ExportModelResult>>(Export(envelope.Message));

    static ExportModelResult Export(ExportModelCommand command) {
        var state = command.State;
        state.Save(DefinitionExport.Of(command.Definition), BlockExport.Of(state, command.Block));
        foreach (var library in command.Libraries) {
            state.SaveLibrary(library);
        }
        return new ExportModelResult([]);
    }
}
