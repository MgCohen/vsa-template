namespace VSA.Architecture;

public sealed class RuleBook {
    internal readonly List<IRuleEntries> Rules = [];
    readonly Root root;

    internal RuleBook(Root root) {
        this.root = root;
    }

    public From For<T>(Depth? depth = null) where T : Layer {
        if (!root.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Layer.Display(typeof(T))}>() names a layer that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), depth, "definition", Rules);
    }
}
