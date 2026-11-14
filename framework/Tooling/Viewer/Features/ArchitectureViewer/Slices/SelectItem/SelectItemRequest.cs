using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.SelectItem;

public sealed record SelectItemRequest(LayoutData Layout, Focus Target) : IRequest<SelectItemResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/select-item";
}
