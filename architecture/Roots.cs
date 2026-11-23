namespace Umpire.Architecture;

public sealed class Roots {
    readonly List<RootPlace> places = [];

    internal IReadOnlyList<RootPlace> Places => places;

    public void Add<T>() where T : Concept, new() => Place(new T(), null);

    public void Add<T>(string folder) where T : Concept, new() => Place(new T(), folder);

    void Place(Concept record, string? folder) {
        var index = places.FindIndex(place => place.Record.GetType() == record.GetType());
        if (index < 0) {
            places.Add(new RootPlace(record, folder));
        } else if (folder is not null) {
            places[index] = new RootPlace(places[index].Record, folder);
        }
    }
}
