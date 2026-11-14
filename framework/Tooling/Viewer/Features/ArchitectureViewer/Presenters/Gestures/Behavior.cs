namespace ArchitectureViewer.Presenters.Gestures;

public abstract class Behavior(Canvas canvas) : IDisposable
{
    protected Canvas Canvas { get; } = canvas;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool disposing);
}
