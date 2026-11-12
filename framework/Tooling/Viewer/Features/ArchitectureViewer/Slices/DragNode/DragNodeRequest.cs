using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.DragNode;

public sealed record DragNodeRequest(
    LayoutData Layout,
    IReadOnlyDictionary<string, Offset> Offsets,
    string NodeId,
    Offset At) : IRequest<DragNodeResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/drag-node";
}
