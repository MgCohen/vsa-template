namespace VSA.Architecture;

public sealed class RuleBook {
    internal readonly List<IRuleEntries> Rules = [];
    readonly Root root;

    internal RuleBook(Root root) {
        this.root = root;
    }

    public From For<T>(Depth? depth = null) where T : Concept => For(typeof(T), depth);

    internal From For(Type concept, Depth? depth) {
        if (!root.Walk().Any(p => p.Is(concept))) {
            throw new InvalidOperationException(
                $"Rules: For<{Concept.Display(concept)}>() names a concept that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(concept, depth, "definition", Rules);
    }
}
