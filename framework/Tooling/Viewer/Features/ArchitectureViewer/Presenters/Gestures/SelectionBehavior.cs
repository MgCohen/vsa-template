using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class SelectionBehavior : Behavior
{
    public SelectionBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.PointerDown += OnPointerDown;
        Canvas.PointerClick += OnPointerClick;
    }

    protected override void Dispose(bool disposing)
    {
        Canvas.PointerDown -= OnPointerDown;
        Canvas.PointerClick -= OnPointerClick;
    }

    private void OnPointerDown(Target? model, PointerInput e)
    {
        switch (model)
        {
            case null or { PortId: not null }:
                break;
            case not null when e.CtrlKey && Canvas.Graph.Selection == model:
                _ = Canvas.Graph.Select(Target.None);
                break;
            default:
                _ = Canvas.Graph.Select(model);
                break;
        }
    }

    private void OnPointerClick(Target? model, PointerInput e)
    {
        if (model is null)
        {
            _ = Canvas.Graph.Select(Target.None);
        }
    }
}
