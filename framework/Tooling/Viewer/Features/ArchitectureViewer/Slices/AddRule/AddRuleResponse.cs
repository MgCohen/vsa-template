using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.AddRule;

public sealed record AddRuleResponse(IReadOnlyList<RuleChange> Changes, LayoutData Data, LayoutData Layout) : IResponse;
