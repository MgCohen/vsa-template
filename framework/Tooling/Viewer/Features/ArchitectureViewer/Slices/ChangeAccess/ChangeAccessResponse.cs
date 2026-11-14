using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ChangeAccess;

public sealed record ChangeAccessResponse(IReadOnlyList<RuleChange> Changes, LayoutData Data, LayoutData Layout)
    : IResponse;
