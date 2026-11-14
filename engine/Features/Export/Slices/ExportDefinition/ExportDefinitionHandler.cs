using Steps.Domain.Shared;
using VSA.Architecture.Data;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Export.Slices.ExportDefinition;

sealed class ExportDefinitionHandler : CommandHandler<ExportDefinitionCommand, ExportDefinitionResult> {
    public const string Written = "VSA3005";

    public override Task<Result<ExportDefinitionResult>> HandleAsync(Envelope<ExportDefinitionCommand> envelope) {
        var command = envelope.Message;
        var file = Path.GetFullPath(command.File);
        var data = DefinitionExport.Of(command.App.Definition);
        DefinitionJson.Save(data, file);
        var message = $"ExportDefinition wrote {file}: {data.Parts.Count} top parts, {data.Rules.Count} rules.";
        return Task.FromResult<Result<ExportDefinitionResult>>(
            new ExportDefinitionResult(file, [new Finding(Written, FindingSeverity.Info, message)]));
    }
}
