using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.ExportDefinition;

public sealed record ExportDefinitionResponse(string Json, string FileName) : IResponse;
