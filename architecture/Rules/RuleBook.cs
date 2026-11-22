namespace Umpire.Architecture;

public sealed class RuleBook {
    internal readonly List<Rule> Rules;
    internal readonly List<Count> Counts;
    internal readonly List<Match> Matches;
    internal readonly List<TraitRule> Traits;
    internal readonly List<PlaceLimit> Places;
    readonly Block block;

    internal RuleBook(Block block) : this(block, null, [], [], [], [], []) { }

    RuleBook(Block block, Type? scope, List<Rule> rules, List<Count> counts, List<Match> matches, List<TraitRule> traits, List<PlaceLimit> places) {
        this.block = block;
        Scope = scope;
        Rules = rules;
        Counts = counts;
        Matches = matches;
        Traits = traits;
        Places = places;
    }

    internal Type? Scope { get; }

    internal RuleBook Of(Type concept) => new(block, concept, Rules, Counts, Matches, Traits, Places);

    internal List<Entry> InScope(List<Entry> entries, string call = "For") {
        if (Scope is { } concept) {
            var under = new Filter(FilterKind.Under, concept);
            foreach (var entry in entries.Where(e => e.Target != concept && !e.Filters.Contains(under))) {
                RefuseOutOfScope(entry.Target, call);
                entry.Filters.Add(under);
            }
        }
        return entries;
    }

    internal void RefuseOutOfScope(Type subject, string call = "For") {
        if (Scope is not { } concept || subject == concept || block.Walk().Any(p => p.Is(subject) && p.Under(concept))) {
            return;
        }
        var scope = Concept.Display(concept);
        throw new InvalidOperationException(
            $"Rules of {scope}: {call}<{Concept.Display(subject)}>() names no part under {scope}. "
            + $"A concept rule has an implicit Under<{scope}>(). "
            + $"Move the rule to the Rules of the architecture, or name a part of {scope}.");
    }

    public From For<T>(Depth? depth = null) where T : Concept {
        if (!block.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Concept.Display(typeof(T))}>() names a concept that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), depth, this);
    }

    public TypeRules Type<T>() => new(typeof(T), this);
}
