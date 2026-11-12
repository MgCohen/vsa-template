using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.AddRule;

public sealed record AddRuleRequest(
    Definition Definition,
    IReadOnlyList<RuleChange> Changes,
    LayoutData Data,
    string Shape,
    Focus From,
    Focus To,
    string Access,
    string Condition,
    string ConditionLayer) : IRequest<AddRuleResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/add-rule";
}
