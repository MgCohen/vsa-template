using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ChangeShape;

public sealed record ChangeShapeRequest(LayoutData Data, string Shape) : IRequest<ChangeShapeResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/change-shape";
}
