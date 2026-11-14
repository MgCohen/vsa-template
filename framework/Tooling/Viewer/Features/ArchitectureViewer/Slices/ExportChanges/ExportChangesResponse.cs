using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ExportChanges;

public sealed record ExportChangesResponse(string Json, string Code) : IResponse;
