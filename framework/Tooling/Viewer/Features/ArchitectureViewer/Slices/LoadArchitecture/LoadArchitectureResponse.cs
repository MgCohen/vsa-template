using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.LoadArchitecture;

public sealed record LoadArchitectureResponse(
    LayoutData Data,
    LayoutData Layout,
    bool Fits,
    IReadOnlyList<string> Shapes)
    : IResponse;
