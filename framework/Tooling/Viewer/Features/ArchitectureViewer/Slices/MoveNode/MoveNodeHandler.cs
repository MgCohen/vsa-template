using ArchitectureViewer.Domain.Layouts;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.MoveNode;

internal sealed class MoveNodeHandler(ILayoutEngine engine) : RequestHandler<MoveNodeRequest, MoveNodeResponse>
{
    public override async Task<Result<MoveNodeResponse>> HandleAsync(Envelope<MoveNodeRequest> envelope)
    {
        MoveNodeRequest request = envelope.Message;
        return new MoveNodeResponse(await engine.Arrange(LayoutMoves.Move(request.Layout, request.NodeId, request.By)));
    }
}
