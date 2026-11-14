using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.ExportDefinition;

internal sealed class ExportDefinitionHandler(InputFiles files)
    : RequestHandler<ExportDefinitionRequest, ExportDefinitionResponse>
{
    public override Task<Result<ExportDefinitionResponse>> HandleAsync(Envelope<ExportDefinitionRequest> envelope)
    {
        ExportDefinitionRequest request = envelope.Message;
        try
        {
            string source = request.Source ?? files.Architecture;
            ExportedArchitecture exported = DefinitionChanges.Export(source, request.Definition, request.Changes);
            ExportDefinitionResponse response = new(exported.Json, exported.FileName);
            return Task.FromResult<Result<ExportDefinitionResponse>>(response);
        }
        catch (InvalidOperationException e)
        {
            return Task.FromResult<Result<ExportDefinitionResponse>>(new Error("viewer.export_rejected", e.Message));
        }
    }
}
