using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.AddLink;

internal sealed class AddLinkHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<AddLinkRequest, AddLinkResponse>
{
    public override async Task<Result<AddLinkResponse>> HandleAsync(Envelope<AddLinkRequest> envelope)
    {
        AddLinkRequest request = envelope.Message;
        INodeShape shape = shapes.Named(request.Shape);
        Edge? link = NewLink.Of(request.Data, shape, request.From, request.To);
        if (link is null || request.Data.Edges.Any(e => NewLink.Same(e, link)))
        {
            return new Error("viewer.link_rejected", "A link goes from an Out port to an In port of another node.");
        }
        LayoutData data = request.Data with { Edges = [.. request.Data.Edges, link] };
        LayoutData layout = await engine.Draw(data, shape);
        return new AddLinkResponse(data, layout, [Links.Change(ChangeKind.AddLink, link)]);
    }
}
