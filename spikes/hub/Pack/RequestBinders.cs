namespace Spike.Pack;

public sealed class RequestBinders(IReadOnlyDictionary<string, RequestBinder> byName) {
    public bool TryGet(string name, out RequestBinder binder) => byName.TryGetValue(name, out binder!);
}
