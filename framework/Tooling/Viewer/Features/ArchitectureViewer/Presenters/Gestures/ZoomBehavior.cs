using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class ZoomBehavior : Behavior
{
    public ZoomBehavior(Canvas canvas)
        : base(canvas)
    {
        Canvas.Wheel += OnWheel;
    }

    protected override void Dispose(bool disposing)
    {
        Canvas.Wheel -= OnWheel;
    }

    private void OnWheel(WheelInput e)
    {
        ZoomOptions options = Canvas.Options.Zoom;
        if (e.DeltaY == 0 || !options.Enabled)
        {
            return;
        }
        double oldZoom = Canvas.Graph.View.Scale;
        double deltaY = options.Inverse ? -e.DeltaY : e.DeltaY;
        double newZoom = deltaY > 0 ? oldZoom * options.ScaleFactor : oldZoom / options.ScaleFactor;
        newZoom = Math.Clamp(newZoom, options.Minimum, options.Maximum);
        if (newZoom < 0 || newZoom == oldZoom)
        {
            return;
        }
        Frame container = Canvas.Graph.Window;
        double widthDiff = (container.Width * newZoom) - (container.Width * oldZoom);
        double heightDiff = (container.Height * newZoom) - (container.Height * oldZoom);
        double xFactor = (e.ClientX - container.Left - Canvas.Graph.View.X) / oldZoom / container.Width;
        double yFactor = (e.ClientY - container.Top - Canvas.Graph.View.Y) / oldZoom / container.Height;
        Viewport view = Canvas.Graph.View;
        Canvas.Graph.ShowView(new Viewport(view.X - (widthDiff * xFactor), view.Y - (heightDiff * yFactor), newZoom));
    }
}
