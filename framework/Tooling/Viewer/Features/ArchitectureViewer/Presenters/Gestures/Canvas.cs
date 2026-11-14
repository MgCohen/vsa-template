using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters.Gestures;

public sealed class Canvas : IDisposable
{
    private readonly Dictionary<Type, Behavior> behaviors = [];

    public Canvas(GraphPresenter graph)
    {
        Graph = graph;
        RegisterBehavior(new SelectionBehavior(this));
        RegisterBehavior(new DragMovablesBehavior(this));
        RegisterBehavior(new DragNewLinkBehavior(this));
        RegisterBehavior(new KeyboardShortcutsBehavior(this));
        RegisterBehavior(new PanBehavior(this));
        RegisterBehavior(new ZoomBehavior(this));
        RegisterBehavior(new EventsBehavior(this));
    }

    public GraphPresenter Graph { get; }

    public CanvasOptions Options { get; set; } = CanvasOptions.Default;

    public event Action<Target?, PointerInput>? PointerDown;

    public event Action<Target?, PointerInput>? PointerMove;

    public event Action<Target?, PointerInput>? PointerUp;

    public event Action<Target?, PointerInput>? PointerClick;

    public event Action<Target?, PointerInput>? PointerDoubleClick;

    public event Action<WheelInput>? Wheel;

    public event Action<KeyInput>? KeyDown;

    public void TriggerPointerDown(Target? target, PointerInput e)
    {
        PointerDown?.Invoke(target, e);
    }

    public void TriggerPointerMove(Target? target, PointerInput e)
    {
        PointerMove?.Invoke(target, e);
    }

    public void TriggerPointerUp(Target? target, PointerInput e)
    {
        PointerUp?.Invoke(target, e);
    }

    public void TriggerPointerClick(Target? target, PointerInput e)
    {
        PointerClick?.Invoke(target, e);
    }

    public void TriggerPointerDoubleClick(Target? target, PointerInput e)
    {
        PointerDoubleClick?.Invoke(target, e);
    }

    public void TriggerWheel(WheelInput e)
    {
        Wheel?.Invoke(e);
    }

    public void TriggerKeyDown(KeyInput e)
    {
        KeyDown?.Invoke(e);
    }

    public void RegisterBehavior(Behavior behavior)
    {
        Type type = behavior.GetType();
        if (!behaviors.TryAdd(type, behavior))
        {
            throw new InvalidOperationException($"Behavior '{type.Name}' already registered");
        }
    }

    public T? GetBehavior<T>()
        where T : Behavior
    {
        return behaviors.GetValueOrDefault(typeof(T)) as T;
    }

    public void UnregisterBehavior<T>()
        where T : Behavior
    {
        if (behaviors.Remove(typeof(T), out Behavior? behavior))
        {
            behavior.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (Behavior behavior in behaviors.Values)
        {
            behavior.Dispose();
        }
        behaviors.Clear();
    }
}
