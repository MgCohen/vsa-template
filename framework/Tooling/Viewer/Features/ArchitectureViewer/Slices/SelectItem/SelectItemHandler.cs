using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.SelectItem;

internal sealed class SelectItemHandler : RequestHandler<SelectItemRequest, SelectItemResponse>
{
    public override Task<Result<SelectItemResponse>> HandleAsync(Envelope<SelectItemRequest> envelope)
    {
        return Task.FromResult<Result<SelectItemResponse>>(Select(envelope.Message.Layout, envelope.Message.Target));
    }

    private static SelectItemResponse Select(LayoutData layout, Focus target)
    {
        if (target.NodeId is null && target.EdgeId is null)
        {
            return SelectItemResponse.Nothing;
        }
        List<Edge> hot = [.. layout.Edges.Where(e => Touches(target, e))];
        if (target.EdgeId is not null && hot.Count == 0)
        {
            return SelectItemResponse.Nothing;
        }
        List<string> related = [.. hot.SelectMany(e => new[] { e.FromNode, e.ToNode })];
        if (target.NodeId is not null)
        {
            related.Add(target.NodeId);
        }
        return new SelectItemResponse(target, [.. hot.Select(e => e.Id)], [.. related.Distinct()]);
    }

    private static bool Touches(Focus target, Edge edge)
    {
        if (target.EdgeId is not null)
        {
            return edge.Id == target.EdgeId;
        }
        return Ends(target, edge.FromNode, edge.FromRow) || Ends(target, edge.ToNode, edge.ToRow);
    }

    private static bool Ends(Focus target, string node, string? row)
    {
        return node == target.NodeId && (target.RowId is null || row == target.RowId);
    }
}
