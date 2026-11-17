namespace Umpire.Architecture;

public abstract class Chain<TSelf> where TSelf : Chain<TSelf> {
    internal readonly List<Entry> Entries = [];

    public TSelf And<T>(Depth? depth = null) where T : Concept => Begin(typeof(T), depth);

    public TSelf And<T>(Access? access) where T : Concept => Begin(typeof(T), null, access);

    public TSelf And<T>(Boundary boundary) where T : Concept => Begin(typeof(T), null, null, boundary);

    public TSelf Under<T>() where T : Concept => Filter(FilterKind.Under, typeof(T));

    internal TSelf Begin(Type concept, Depth? depth, Access? access = null, Boundary? boundary = null) {
        Entries.Add(new Entry(concept, depth, access, boundary));
        return (TSelf)this;
    }

    internal TSelf Filter(FilterKind kind, Type concept) {
        foreach (var entry in Entries) {
            entry.Filters.Add(new Filter(kind, concept));
        }
        return (TSelf)this;
    }
}
