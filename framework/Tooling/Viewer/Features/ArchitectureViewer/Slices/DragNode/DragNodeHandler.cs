using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.DragNode;

internal sealed class DragNodeHandler : RequestHandler<DragNodeRequest, DragNodeResponse>
{
    public override Task<Result<DragNodeResponse>> HandleAsync(Envelope<DragNodeRequest> envelope)
    {
        DragNodeRequest request = envelope.Message;
        Dictionary<string, Offset> offsets = new(request.Offsets) { [request.NodeId] = request.At };
        Dictionary<string, Node> nodes = request.Layout.Nodes.ToDictionary(n => n.Id);
        List<Edge> rerouted = [.. request.Layout.Edges
            .Where(e => e.FromNode == request.NodeId || e.ToNode == request.NodeId)
            .Select(e => e with { Route = Moved(e, nodes, offsets) })];
        return Task.FromResult<Result<DragNodeResponse>>(new DragNodeResponse(request.NodeId, request.At, rerouted));
    }

    private static IReadOnlyList<double[]> Moved(
        Edge edge, Dictionary<string, Node> nodes, Dictionary<string, Offset> offsets)
    {
        Offset from = offsets.GetValueOrDefault(edge.FromNode, Offset.Zero);
        Offset to = offsets.GetValueOrDefault(edge.ToNode, Offset.Zero);
        return EdgeRoutes.Moved(edge, nodes[edge.FromNode], nodes[edge.ToNode], from, to);
    }
}
