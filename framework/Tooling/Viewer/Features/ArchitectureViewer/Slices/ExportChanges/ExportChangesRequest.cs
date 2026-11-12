using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ExportChanges;

public sealed record ExportChangesRequest(
    Definition Definition,
    IReadOnlyList<RuleChange> Changes,
    IReadOnlyList<LinkChange> Links,
    LayoutData App) : IRequest<ExportChangesResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/export-changes";
}
