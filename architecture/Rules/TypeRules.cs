namespace Umpire.Architecture;

public sealed class TypeRules {
    readonly Type type;
    readonly RuleBook book;

    internal TypeRules(Type type, RuleBook book) {
        this.type = type;
        this.book = book;
    }

    public PlaceLimit OnlyIn<T>(Depth? depth = null) where T : Concept {
        var limit = new PlaceLimit(type, typeof(T), depth, book);
        book.Places.Add(limit);
        return limit;
    }
}
