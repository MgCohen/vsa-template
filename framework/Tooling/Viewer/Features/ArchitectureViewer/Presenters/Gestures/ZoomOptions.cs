namespace ArchitectureViewer.Presenters.Gestures;

public sealed record ZoomOptions(bool Enabled, bool Inverse, double Minimum, double Maximum, double ScaleFactor)
{
    public static readonly ZoomOptions Default = new(true, true, 0.2, 4, 1.1);
}
