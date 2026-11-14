using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.RemoveLink;

public sealed record RemoveLinkResponse(LayoutData Data, LayoutData Layout, IReadOnlyList<LinkChange> Changes)
    : IResponse;
