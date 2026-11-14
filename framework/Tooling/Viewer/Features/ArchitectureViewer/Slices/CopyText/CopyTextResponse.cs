using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.CopyText;

public sealed record CopyTextResponse(bool Copied) : IResponse;
