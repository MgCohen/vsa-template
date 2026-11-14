namespace VSA.Architecture;

public abstract class Chain<TSelf> where TSelf : Chain<TSelf> {
    internal readonly List<Entry> Entries = [];
    int runStart;
    bool filtered;

    public TSelf And<T>(Depth? depth = null) where T : Concept => And<T>(null, depth);

    public TSelf And<T>(Access? access, Depth? depth = null) where T : Concept => And<T>(access, depth, null);

    public TSelf And<T>(Boundary boundary, Access? access = null, Depth? depth = null) where T : Concept => And<T>(access, depth, boundary);

    TSelf And<T>(Access? access, Depth? depth, Boundary? boundary) where T : Concept {
        if (filtered) {
            runStart = Entries.Count;
        }
        filtered = false;
        return Begin(typeof(T), depth, access, boundary);
    }

    public TSelf Under<T>() where T : Concept => Filter("Under", typeof(T));

    internal TSelf Begin(Type concept, Depth? depth, Access? access = null, Boundary? boundary = null) {
        Entries.Add(new Entry(concept, depth, access, boundary));
        return (TSelf)this;
    }

    internal TSelf Filter(string kind, Type concept) {
        foreach (var entry in Entries.Skip(runStart)) {
            entry.Filters.Add(new Filter(kind, concept));
        }
        filtered = true;
        return (TSelf)this;
    }
}
