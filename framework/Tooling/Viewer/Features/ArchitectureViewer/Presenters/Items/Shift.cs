namespace ArchitectureViewer.Presenters.Items;

public readonly record struct Shift(double X, double Y)
{
    public static readonly Shift Zero = new(0, 0);
}
