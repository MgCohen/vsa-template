using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class DragNewLinkBehavior : Behavior
{
    public DragNewLinkBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.PointerDown += OnPointerDown;
        Canvas.PointerMove += OnPointerMove;
        Canvas.PointerUp += OnPointerUp;
    }

    public LinkDraft? OngoingLink => Canvas.Graph.Draft;

    protected override void Dispose(bool disposing)
    {
        Canvas.PointerDown -= OnPointerDown;
        Canvas.PointerMove -= OnPointerMove;
        Canvas.PointerUp -= OnPointerUp;
    }

    private void OnPointerDown(Target? model, PointerInput e)
    {
        if (e.Button != (long)MouseButton.Left || !Canvas.Graph.Editing || model is null)
        {
            return;
        }
        if (Canvas.Graph.PortOf(model)?.Out == true)
        {
            Canvas.Graph.StartLink(model, CalculateTargetPosition(e.ClientX, e.ClientY));
        }
    }

    private void OnPointerMove(Target? model, PointerInput e)
    {
        if (OngoingLink == null || model != null)
        {
            return;
        }
        Canvas.Graph.DrawLink(CalculateTargetPosition(e.ClientX, e.ClientY));
    }

    private void OnPointerUp(Target? model, PointerInput e)
    {
        if (OngoingLink == null)
        {
            return;
        }
        bool onInPort = model is not null && Canvas.Graph.PortOf(model) is { Out: false };
        _ = Canvas.Graph.EndLink(onInPort ? model : null);
    }

    private double[] CalculateTargetPosition(double clientX, double clientY)
    {
        double[] target = Canvas.Graph.ToCanvas(clientX, clientY);
        if (OngoingLink == null)
        {
            return target;
        }
        double[] source = OngoingLink.Start;
        double length = Math.Max(Math.Sqrt(
            Math.Pow(target[0] - source[0], 2) + Math.Pow(target[1] - source[1], 2)), 0.001);
        return [target[0] - ((target[0] - source[0]) / length * 5), target[1] - ((target[1] - source[1]) / length * 5)];
    }
}
