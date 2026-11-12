using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Presenters.Items;
using ArchitectureViewer.Slices.AddLink;
using ArchitectureViewer.Slices.DragNode;
using ArchitectureViewer.Slices.MoveNode;
using ArchitectureViewer.Slices.RemoveLink;
using VSA.Contracts;

namespace ArchitectureViewer.Presenters;

public sealed partial class GraphPresenter
{
    public LinkDraft? Draft { get; private set; }

    public async Task DragNode(string nodeId, Shift at)
    {
        DragNodeRequest request = new(layout, new Dictionary<string, Offset>(offsets), nodeId, new Offset(at.X, at.Y));
        Result<DragNodeResponse> dragged = await Bus.Send(request);
        if (!dragged.IsSuccess)
        {
            return;
        }
        offsets[nodeId] = dragged.Value.At;
        foreach (Edge edge in dragged.Value.Rerouted)
        {
            routes[edge.Id] = edge.Route;
        }
        await Notify();
    }

    public async Task MoveNode(string nodeId)
    {
        MoveNodeRequest request = new(layout, nodeId, offsets.GetValueOrDefault(nodeId));
        Result<MoveNodeResponse> moved = await Bus.Send(request);
        if (moved.IsSuccess)
        {
            await Show(moved.Value.Layout);
        }
    }

    public PortItem? PortOf(Target target)
    {
        CardItem? card = Cards.FirstOrDefault(c => c.Id == target.NodeId);
        return target.PortId is null ? null : card?.Ports.FirstOrDefault(p => p.Id == target.PortId);
    }

    public void StartLink(Target from, double[] end)
    {
        CardItem card = Cards.First(c => c.Id == from.NodeId);
        PortItem port = PortOf(from)!;
        Draft = new LinkDraft(from, [card.X + (port.Out ? card.W + 1 : -1), card.Y + port.Y + 0.5], end);
    }

    public void DrawLink(double[] end)
    {
        Draft = Draft is null ? null : Draft with { End = end };
    }

    public async Task EndLink(Target? to)
    {
        Target? from = Draft?.From;
        Draft = null;
        await Notify();
        if (from is null || to?.PortId is null)
        {
            return;
        }
        if (Kind == GraphKind.Definition)
        {
            await AddRule(from, to);
            return;
        }
        AddLinkRequest request = new(data, Shape, Mapping.FocusOf(from), Mapping.FocusOf(to));
        Result<AddLinkResponse> added = await Bus.Send(request);
        if (added.IsSuccess)
        {
            await Apply(added.Value.Data, added.Value.Layout, added.Value.Changes);
        }
    }

    public async Task RemoveLink(string edgeId)
    {
        if (Kind == GraphKind.Definition)
        {
            await OnOneRule(edgeId, RemoveRule);
            return;
        }
        Result<RemoveLinkResponse> removed = await Bus.Send(new RemoveLinkRequest(data, Shape, edgeId));
        if (removed.IsSuccess)
        {
            await Apply(removed.Value.Data, removed.Value.Layout, removed.Value.Changes);
        }
    }

    public Shift OffsetOf(string nodeId)
    {
        Offset at = offsets.GetValueOrDefault(nodeId, Offset.Zero);
        return new Shift(at.X, at.Y);
    }

    public IReadOnlyList<double[]> PointsOf(LinkItem link)
    {
        return routes.GetValueOrDefault(link.Id, link.Route);
    }

    private async Task Apply(LayoutData changed, LayoutData shown, IReadOnlyList<LinkChange> made)
    {
        data = changed;
        changes.AddRange(made);
        await Show(shown);
    }
}
