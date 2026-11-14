using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.RemoveLink;

internal sealed class RemoveLinkHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<RemoveLinkRequest, RemoveLinkResponse>
{
    public override async Task<Result<RemoveLinkResponse>> HandleAsync(Envelope<RemoveLinkRequest> envelope)
    {
        RemoveLinkRequest request = envelope.Message;
        Edge? edge = request.Data.Edges.FirstOrDefault(e => e.Id == request.EdgeId);
        if (edge is null)
        {
            return new Error("viewer.link_not_found", $"No link {request.EdgeId}.");
        }
        INodeShape shape = shapes.Named(request.Shape);
        Dictionary<string, Node> nodes = request.Data.Nodes.Select(shape.Shape).ToDictionary(n => n.Id);
        (string, string, string, string) key = Shaping.Key(edge, nodes);
        List<Edge> removed = [.. request.Data.Edges.Where(e => Shaping.Key(e, nodes) == key)];
        LayoutData data = request.Data with { Edges = [.. request.Data.Edges.Except(removed)] };
        List<LinkChange> changes = [.. removed.Select(e => Links.Change(ChangeKind.RemoveLink, e))];
        return new RemoveLinkResponse(data, await engine.Draw(data, shape), changes);
    }
}
