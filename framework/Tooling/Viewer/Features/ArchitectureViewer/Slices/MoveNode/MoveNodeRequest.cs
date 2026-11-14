using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.MoveNode;

public sealed record MoveNodeRequest(LayoutData Layout, string NodeId, Offset By) : IRequest<MoveNodeResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/move-node";
}
