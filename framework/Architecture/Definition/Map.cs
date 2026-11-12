namespace VSA.Architecture;

public sealed class Map {
    internal readonly List<IRuleEntries> Rules = [];
    readonly Root root;

    internal Map(Root root) {
        this.root = root;
    }

    public From For<T>(Reach? reach = null) where T : Layer {
        if (!root.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Layer.Display(typeof(T))}>() names a layer that the map does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), reach, "definition", Rules);
    }
}
