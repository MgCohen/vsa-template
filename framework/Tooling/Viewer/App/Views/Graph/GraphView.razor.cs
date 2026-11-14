using ArchitectureViewer.Presenters;
using ArchitectureViewer.Presenters.Items;
using App.Views.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace App.Views.Graph;

public partial class GraphView
{
    private GraphPresenter Graph => Presenter;

    private string Transform => Svg.Format($"translate({Graph.View.X},{Graph.View.Y}) scale({Graph.View.Scale})");

    private static string DraftPath(LinkDraft draft)
    {
        return Svg.Format($"M{draft.Start[0]},{draft.Start[1]}L{draft.End[0]},{draft.End[1]}");
    }

    private string? SelectedRowOf(string nodeId)
    {
        return Graph.Selection.NodeId == nodeId ? Graph.Selection.RowId ?? "*" : null;
    }

    private void Down(PointerEventArgs e)
    {
        Graph.Input.TriggerPointerDown(null, e.ToInput());
    }

    private void Move(PointerEventArgs e)
    {
        Graph.Input.TriggerPointerMove(null, e.ToInput());
    }

    private void Up(PointerEventArgs e)
    {
        Graph.Input.TriggerPointerUp(null, e.ToInput());
    }

    private void Wheel(WheelEventArgs e)
    {
        Graph.Input.TriggerWheel(e.ToInput());
    }

    private void Key(KeyboardEventArgs e)
    {
        Graph.Input.TriggerKeyDown(e.ToInput());
    }

    private void DownOn((Target Target, PointerEventArgs Pointer) e)
    {
        Graph.Input.TriggerPointerDown(e.Target, e.Pointer.ToInput());
    }

    private void UpOn((Target Target, PointerEventArgs Pointer) e)
    {
        Graph.Input.TriggerPointerUp(e.Target, e.Pointer.ToInput());
    }

    private Dictionary<string, object?> ParametersOf(CardItem card)
    {
        return new()
        {
            ["Card"] = card,
            ["At"] = Graph.OffsetOf(card.Id),
            ["Dimmed"] = Graph.IsDimmed(card.Id),
            ["SelectedRow"] = SelectedRowOf(card.Id),
            ["Edit"] = Graph.Editing,
            ["OnDown"] = EventCallback.Factory.Create<(Target, PointerEventArgs)>(this, DownOn),
            ["OnUp"] = EventCallback.Factory.Create<(Target, PointerEventArgs)>(this, UpOn),
        };
    }
}
