using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.LoadDefinition;

public sealed record LoadDefinitionResponse(Definition Definition, LayoutData Data, LayoutData Layout) : IResponse;
