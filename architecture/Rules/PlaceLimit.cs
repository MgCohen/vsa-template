namespace Umpire.Architecture;

public sealed class PlaceLimit : Chain<PlaceLimit> {
    readonly RuleBook book;

    internal PlaceLimit(Type type, Type concept, Depth? depth, RuleBook book) {
        Type = type;
        this.book = book;
        Begin(concept, depth);
    }

    internal Type Type { get; }

    internal List<Entry> Places => book.InScope(Entries, "OnlyIn");

    public override string ToString() => $"{TypeText.Of(Type)} is only in {string.Join(" and ", Entries)}";
}
