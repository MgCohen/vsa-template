using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.LoadArchitecture;

internal sealed class LoadArchitectureHandler(ArchitectureFiles files, IEnumerable<INodeShape> shapes)
    : RequestHandler<LoadArchitectureRequest, LoadArchitectureResponse>
{
    public override Task<Result<LoadArchitectureResponse>> HandleAsync(Envelope<LoadArchitectureRequest> envelope)
    {
        LayoutData data = LayoutJson.Read(files.LayoutJson);
        INodeShape shape = shapes.Named(envelope.Message.Shape);
        LayoutData layout = Shaping.Apply(data, shape);
        LoadArchitectureResponse response = new(data, layout, Shaping.Fits(data, shape), shapes.Names());
        return Task.FromResult<Result<LoadArchitectureResponse>>(response);
    }
}
