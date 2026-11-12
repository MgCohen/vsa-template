namespace Signals.Slices.Ring;

// A concrete helper class in Handlers.Events that is not an event handler. The host must not register it as a handler.
public sealed class RingLog {
    readonly List<string> bells = [];

    public IReadOnlyList<string> Bells => bells;

    public void Add(string bell) => bells.Add(bell);
}
