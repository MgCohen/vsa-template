using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.ChangeShape;

internal sealed class ChangeShapeHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<ChangeShapeRequest, ChangeShapeResponse>
{
    public override async Task<Result<ChangeShapeResponse>> HandleAsync(Envelope<ChangeShapeRequest> envelope)
    {
        ChangeShapeRequest request = envelope.Message;
        return new ChangeShapeResponse(await engine.Draw(request.Data, shapes.Named(request.Shape)));
    }
}
