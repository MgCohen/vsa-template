using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.AddLink;

public sealed record AddLinkRequest(LayoutData Data, string Shape, Focus From, Focus To) : IRequest<AddLinkResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/add-link";
}
