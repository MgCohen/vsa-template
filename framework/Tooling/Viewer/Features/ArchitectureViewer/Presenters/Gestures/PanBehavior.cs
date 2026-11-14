using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class PanBehavior : Behavior
{
    private Viewport? initialPan;
    private double lastClientX;
    private double lastClientY;

    public PanBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.PointerDown += OnPointerDown;
        Canvas.PointerMove += OnPointerMove;
        Canvas.PointerUp += OnPointerUp;
    }

    protected override void Dispose(bool disposing)
    {
        Canvas.PointerDown -= OnPointerDown;
        Canvas.PointerMove -= OnPointerMove;
        Canvas.PointerUp -= OnPointerUp;
    }

    private void OnPointerDown(Target? model, PointerInput e)
    {
        if (e.Button == (long)MouseButton.Left)
        {
            Start(model, e.ClientX, e.ClientY, e.ShiftKey);
        }
    }

    private void OnPointerMove(Target? model, PointerInput e)
    {
        Move(e.ClientX, e.ClientY);
    }

    private void OnPointerUp(Target? model, PointerInput e)
    {
        End();
    }

    private void Start(Target? model, double clientX, double clientY, bool shiftKey)
    {
        if (!Canvas.Options.AllowPanning || (model is not null && Canvas.Graph.Editing) || shiftKey)
        {
            return;
        }
        initialPan = Canvas.Graph.View;
        lastClientX = clientX;
        lastClientY = clientY;
    }

    private void Move(double clientX, double clientY)
    {
        if (!Canvas.Options.AllowPanning || initialPan is null)
        {
            return;
        }
        double deltaX = clientX - lastClientX - (Canvas.Graph.View.X - initialPan.X);
        double deltaY = clientY - lastClientY - (Canvas.Graph.View.Y - initialPan.Y);
        Canvas.Graph.Pan(deltaX, deltaY);
    }

    private void End()
    {
        if (Canvas.Options.AllowPanning)
        {
            initialPan = null;
        }
    }
}
