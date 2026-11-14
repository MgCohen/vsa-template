using System.Diagnostics;
using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class EventsBehavior : Behavior
{
    private readonly Stopwatch clickWatch = new();
    private Target? target;
    private bool captureMove;
    private int moveCount;

    public EventsBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.PointerDown += OnPointerDown;
        Canvas.PointerMove += OnPointerMove;
        Canvas.PointerUp += OnPointerUp;
        Canvas.PointerClick += OnPointerClick;
    }

    protected override void Dispose(bool disposing)
    {
        Canvas.PointerDown -= OnPointerDown;
        Canvas.PointerMove -= OnPointerMove;
        Canvas.PointerUp -= OnPointerUp;
        Canvas.PointerClick -= OnPointerClick;
        target = null;
    }

    private void OnPointerClick(Target? model, PointerInput e)
    {
        if (clickWatch.IsRunning && clickWatch.ElapsedMilliseconds <= 500)
        {
            Canvas.TriggerPointerDoubleClick(model, e);
        }
        clickWatch.Restart();
    }

    private void OnPointerDown(Target? model, PointerInput e)
    {
        captureMove = true;
        moveCount = 0;
        target = model;
    }

    private void OnPointerMove(Target? model, PointerInput e)
    {
        if (captureMove)
        {
            moveCount++;
        }
    }

    private void OnPointerUp(Target? model, PointerInput e)
    {
        if (!captureMove)
        {
            return;
        }
        captureMove = false;
        if (moveCount > 0 || target != model)
        {
            return;
        }
        Canvas.TriggerPointerClick(model, e);
        target = null;
    }
}
