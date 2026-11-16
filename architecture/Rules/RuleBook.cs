namespace Umpire.Architecture;

public sealed class RuleBook {
    internal readonly List<Rule> Rules = [];
    internal readonly List<Count> Counts = [];
    internal readonly List<Match> Matches = [];
    readonly Root root;

    internal RuleBook(Root root) {
        this.root = root;
    }

    public From For<T>(Depth? depth = null) where T : Concept {
        if (!root.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Concept.Display(typeof(T))}>() names a concept that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), depth, this);
    }
}
