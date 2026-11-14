using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.RemoveLink;

public sealed record RemoveLinkRequest(LayoutData Data, string Shape, string EdgeId) : IRequest<RemoveLinkResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/remove-link";
}
