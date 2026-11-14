using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ExportDefinition;

public sealed record ExportDefinitionRequest(
    string? Source,
    Definition Definition,
    IReadOnlyList<RuleChange> Changes) : IRequest<ExportDefinitionResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/export-definition";
}
