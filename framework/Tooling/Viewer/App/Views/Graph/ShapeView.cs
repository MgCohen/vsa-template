using ArchitectureViewer.Presenters.Items;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace App.Views.Graph;

public abstract class ShapeView : ComponentBase
{
    private object? drawn;

    [Parameter, EditorRequired] public CardItem Card { get; set; } = default!;
    [Parameter] public Shift At { get; set; }
    [Parameter] public bool Dimmed { get; set; }
    [Parameter] public string? SelectedRow { get; set; }
    [Parameter] public bool Edit { get; set; }
    [Parameter] public EventCallback<(Target Target, PointerEventArgs Pointer)> OnDown { get; set; }
    [Parameter] public EventCallback<(Target Target, PointerEventArgs Pointer)> OnUp { get; set; }

    protected string Color => Drawing.Theme.LayerColor(Card.Layer);

    private object State => (Card, At, Dimmed, SelectedRow, Edit);

    protected override bool ShouldRender()
    {
        return !Equals(drawn, State);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        drawn = State;
    }

    protected Task Down(Target target, PointerEventArgs e)
    {
        return OnDown.InvokeAsync((target, e));
    }

    protected Task Up(Target target, PointerEventArgs e)
    {
        return OnUp.InvokeAsync((target, e));
    }
}
