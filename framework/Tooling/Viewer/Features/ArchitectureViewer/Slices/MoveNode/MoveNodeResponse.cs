using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.MoveNode;

public sealed record MoveNodeResponse(LayoutData Layout) : IResponse;
