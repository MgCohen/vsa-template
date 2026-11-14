namespace ArchitectureViewer.Presenters.Items;

public sealed record Viewport(double X, double Y, double Scale)
{
    public static Viewport Fit(double width, double height, Frame window)
    {
        return new Viewport(10, 10, Math.Min(1, Math.Min(window.Width / width, window.Height / height)));
    }

    public Viewport Pan(double dx, double dy)
    {
        return this with { X = X + dx, Y = Y + dy };
    }
}
