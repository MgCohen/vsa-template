using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ChangeShape;

public sealed record ChangeShapeResponse(LayoutData Layout) : IResponse;
