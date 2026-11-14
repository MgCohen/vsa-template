namespace ArchitectureViewer.Presenters.Gestures;

public sealed record CanvasOptions(bool AllowPanning, ZoomOptions Zoom)
{
    public static readonly CanvasOptions Default = new(true, ZoomOptions.Default);
}
