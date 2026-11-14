using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.DragNode;

public sealed record DragNodeResponse(string NodeId, Offset At, IReadOnlyList<Edge> Rerouted) : IResponse;
