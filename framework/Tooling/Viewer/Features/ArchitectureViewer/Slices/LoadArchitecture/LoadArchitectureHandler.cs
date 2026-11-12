using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.LoadArchitecture;

internal sealed class LoadArchitectureHandler(
    ArchitectureFiles files, ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<LoadArchitectureRequest, LoadArchitectureResponse>
{
    public override async Task<Result<LoadArchitectureResponse>> HandleAsync(
        Envelope<LoadArchitectureRequest> envelope)
    {
        LayoutData data = AppGraph.Of(files.ArchitectureJson);
        LayoutData layout = await engine.Draw(data, shapes.Named(envelope.Message.Shape));
        return new LoadArchitectureResponse(data, layout, true, shapes.Names());
    }
}
