namespace ArchitectureViewer.Domain.Shared;

public readonly record struct Offset(double X, double Y)
{
    public static readonly Offset Zero = new(0, 0);
}
