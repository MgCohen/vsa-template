using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.AddLink;

public sealed record AddLinkResponse(LayoutData Data, LayoutData Layout, IReadOnlyList<LinkChange> Changes) : IResponse;
