namespace Umpire.Architecture;

public sealed class Roots {
    readonly List<Concept> records = [];

    internal IReadOnlyList<Concept> Records => records;

    public void Add<T>() where T : Concept, new() {
        if (!records.Any(record => record.GetType() == typeof(T))) {
            records.Add(new T());
        }
    }
}
