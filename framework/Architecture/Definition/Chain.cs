namespace VSA.Architecture;

public abstract class Chain<TSelf> where TSelf : Chain<TSelf> {
    internal readonly List<Entry> Entries = [];
    int runStart;
    bool filtered;

    public TSelf And<T>(Reach? reach = null) where T : Layer {
        if (filtered) {
            runStart = Entries.Count;
        }
        filtered = false;
        return Begin(typeof(T), reach);
    }

    public TSelf Under<T>() where T : Layer => Filter("Under", typeof(T));

    internal TSelf Begin(Type layer, Reach? reach) {
        Entries.Add(new Entry(layer, reach));
        return (TSelf)this;
    }

    internal TSelf Filter(string kind, Type layer) {
        foreach (var entry in Entries.Skip(runStart)) {
            entry.Filters.Add(new Filter(kind, layer));
        }
        filtered = true;
        return (TSelf)this;
    }
}
