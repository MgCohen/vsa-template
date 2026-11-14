using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.ExportDefinition;

internal sealed class ExportDefinitionHandler(ArchitectureFiles files)
    : RequestHandler<ExportDefinitionRequest, ExportDefinitionResponse>
{
    public override Task<Result<ExportDefinitionResponse>> HandleAsync(Envelope<ExportDefinitionRequest> envelope)
    {
        ExportDefinitionRequest request = envelope.Message;
        try
        {
            string source = request.Source ?? files.DefinitionJson;
            string json = DefinitionChanges.Apply(source, request.Definition, request.Changes);
            return Task.FromResult<Result<ExportDefinitionResponse>>(new ExportDefinitionResponse(json));
        }
        catch (InvalidOperationException e)
        {
            return Task.FromResult<Result<ExportDefinitionResponse>>(new Error("viewer.export_rejected", e.Message));
        }
    }
}
