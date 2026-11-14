using System.Text.Json;
using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.LoadDefinition;

internal sealed class LoadDefinitionHandler(
    InputFiles files, ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<LoadDefinitionRequest, LoadDefinitionResponse>
{
    public override async Task<Result<LoadDefinitionResponse>> HandleAsync(Envelope<LoadDefinitionRequest> envelope)
    {
        string json = envelope.Message.Json ?? files.Architecture;
        Definition definition;
        try
        {
            definition = ArchitectureImport.Read(json);
        }
        catch (JsonException e)
        {
            return new Error("viewer.definition_not_valid", e.Message);
        }
        LayoutData data = DefinitionGraph.Of(definition, []);
        LayoutData layout = await engine.Draw(data, shapes.Named(envelope.Message.Shape));
        return new LoadDefinitionResponse(definition, data, layout, SnapshotCheck.Warning(files.Snapshot, json));
    }
}
