using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.RemoveRule;

public sealed record RemoveRuleRequest(
    Definition Definition,
    IReadOnlyList<RuleChange> Changes,
    LayoutData Data,
    string Shape,
    string EdgeId) : IRequest<RemoveRuleResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/remove-rule";
}
