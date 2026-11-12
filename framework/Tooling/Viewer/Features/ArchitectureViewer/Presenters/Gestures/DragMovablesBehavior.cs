using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class DragMovablesBehavior : Behavior
{
    private readonly Dictionary<string, Shift> initialPositions = [];
    private double? lastClientX;
    private double? lastClientY;
    private bool moved;

    public DragMovablesBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.PointerDown += OnPointerDown;
        Canvas.PointerMove += OnPointerMove;
        Canvas.PointerUp += OnPointerUp;
    }

    protected override void Dispose(bool disposing)
    {
        initialPositions.Clear();
        Canvas.PointerDown -= OnPointerDown;
        Canvas.PointerMove -= OnPointerMove;
        Canvas.PointerUp -= OnPointerUp;
    }

    private void OnPointerDown(Target? model, PointerInput e)
    {
        if (!Canvas.Graph.Editing || model is not { RowId: null, PortId: null, NodeId: not null })
        {
            return;
        }
        initialPositions.Clear();
        if (Canvas.Graph.Selection is { RowId: null, NodeId: { } nodeId })
        {
            initialPositions.Add(nodeId, Canvas.Graph.OffsetOf(nodeId));
        }
        lastClientX = e.ClientX;
        lastClientY = e.ClientY;
        moved = false;
    }

    private void OnPointerMove(Target? model, PointerInput e)
    {
        if (initialPositions.Count == 0 || lastClientX is null || lastClientY is null)
        {
            return;
        }
        moved = true;
        double deltaX = (e.ClientX - lastClientX.Value) / Canvas.Graph.View.Scale;
        double deltaY = (e.ClientY - lastClientY.Value) / Canvas.Graph.View.Scale;
        foreach ((string nodeId, Shift initialPosition) in initialPositions)
        {
            _ = Canvas.Graph.DragNode(nodeId, new Shift(initialPosition.X + deltaX, initialPosition.Y + deltaY));
        }
    }

    private void OnPointerUp(Target? model, PointerInput e)
    {
        if (initialPositions.Count == 0)
        {
            return;
        }
        if (moved)
        {
            foreach (string nodeId in initialPositions.Keys)
            {
                _ = Canvas.Graph.MoveNode(nodeId);
            }
        }
        initialPositions.Clear();
        lastClientX = null;
        lastClientY = null;
    }
}
