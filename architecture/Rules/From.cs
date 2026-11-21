namespace Umpire.Architecture;

public sealed class From : Chain<From> {
    readonly RuleBook book;

    internal From(Type concept, Depth? depth, RuleBook book) {
        this.book = book;
        Begin(concept, depth);
    }

    public Rule Uses<T>(Depth? depth = null) where T : Concept => Start(typeof(T), Permission.Allow, depth, null, null);

    public Rule Uses<T>(Access? access) where T : Concept => Start(typeof(T), Permission.Allow, null, access, null);

    public Rule Uses<T>(Boundary boundary) where T : Concept => Start(typeof(T), Permission.Allow, null, null, boundary);

    public Rule Denies<T>(Depth? depth = null) where T : Concept => Start(typeof(T), Permission.Deny, depth, null, null);

    public Count Has<T>(Depth? depth = null) where T : Concept {
        var count = new Count(Subjects(), typeof(T), depth);
        book.Counts.Add(count);
        return count;
    }

    public Match Takes<T>() where T : Concept {
        var match = new Match(Subjects(), typeof(T));
        book.Matches.Add(match);
        return match;
    }

    public void OnlyKind(TypeKind kinds) => book.Kinds.Add(new KindLimit(Subjects(), kinds));

    public void Owns<TAttribute>() where TAttribute : Attribute => book.Owners.Add(new Owner(Subjects(), typeof(TAttribute)));

    Rule Start(Type to, Permission permission, Depth? depth, Access? access, Boundary? boundary) {
        var rule = new Rule { Permission = permission, From = Subjects() };
        rule.Begin(to, depth, access, boundary);
        book.Rules.Add(rule);
        return rule;
    }

    List<Entry> Subjects() {
        if (book.Scope is { } concept) {
            var under = new Filter(FilterKind.Under, concept);
            foreach (var entry in Entries.Where(e => e.Target != concept && !e.Filters.Contains(under))) {
                book.RefuseOutOfScope(entry.Target);
                entry.Filters.Add(under);
            }
        }
        return Entries;
    }
}
