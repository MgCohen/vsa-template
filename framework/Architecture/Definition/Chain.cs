namespace VSA.Architecture;

public abstract class Chain<TSelf> where TSelf : Chain<TSelf> {
    internal readonly List<Entry> Entries = [];
    int runStart;
    bool filtered;

    public TSelf And<T>(Reach? reach = null) where T : Layer => And<T>(null, reach);

    public TSelf And<T>(Access? access, Reach? reach = null) where T : Layer => And<T>(access, reach, null);

    public TSelf And<T>(Isolation isolation, Access? access = null, Reach? reach = null) where T : Layer => And<T>(access, reach, isolation);

    TSelf And<T>(Access? access, Reach? reach, Isolation? isolation) where T : Layer {
        if (filtered) {
            runStart = Entries.Count;
        }
        filtered = false;
        return Begin(typeof(T), reach, access, isolation);
    }

    public TSelf Under<T>() where T : Layer => Filter("Under", typeof(T));

    internal TSelf Begin(Type layer, Reach? reach, Access? access = null, Isolation? isolation = null) {
        Entries.Add(new Entry(layer, reach, access, isolation));
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
