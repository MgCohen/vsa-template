using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.RemoveRule;

public sealed record RemoveRuleResponse(IReadOnlyList<RuleChange> Changes, LayoutData Data, LayoutData Layout)
    : IResponse;
