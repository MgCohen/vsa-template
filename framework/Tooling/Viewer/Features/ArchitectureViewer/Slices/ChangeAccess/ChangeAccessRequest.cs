using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ChangeAccess;

public sealed record ChangeAccessRequest(
    Definition Definition,
    IReadOnlyList<RuleChange> Changes,
    LayoutData Data,
    string Shape,
    string EdgeId) : IRequest<ChangeAccessResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/change-access";
}
