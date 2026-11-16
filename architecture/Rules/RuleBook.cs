namespace Umpire.Architecture;

public sealed class RuleBook {
    internal readonly List<Rule> Rules = [];
    internal readonly List<Count> Counts = [];
    internal readonly List<Match> Matches = [];
    readonly Block block;

    internal RuleBook(Block block) {
        this.block = block;
    }

    public From For<T>(Depth? depth = null) where T : Concept {
        if (!block.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Concept.Display(typeof(T))}>() names a concept that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), depth, this);
    }
}
