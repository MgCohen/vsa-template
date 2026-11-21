namespace Umpire.Architecture;

public sealed class PlaceLimit : Chain<PlaceLimit> {
    internal PlaceLimit(Type type, Type concept, Depth? depth) {
        Type = type;
        Begin(concept, depth);
    }

    internal Type Type { get; }

    public override string ToString() => $"{TypeText.Of(Type)} is only in {string.Join(" and ", Entries)}";
}
