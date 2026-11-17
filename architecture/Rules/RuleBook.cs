namespace Umpire.Architecture;

public sealed class RuleBook {
    internal readonly List<Rule> Rules;
    internal readonly List<Count> Counts;
    internal readonly List<Match> Matches;
    readonly Block block;

    internal RuleBook(Block block) : this(block, null, [], [], []) { }

    RuleBook(Block block, Type? scope, List<Rule> rules, List<Count> counts, List<Match> matches) {
        this.block = block;
        Scope = scope;
        Rules = rules;
        Counts = counts;
        Matches = matches;
    }

    internal Type? Scope { get; }

    internal RuleBook Of(Type concept) => new(block, concept, Rules, Counts, Matches);

    public From For<T>(Depth? depth = null) where T : Concept {
        if (!block.Walk().Any(p => p.Is(typeof(T)))) {
            throw new InvalidOperationException(
                $"Rules: For<{Concept.Display(typeof(T))}>() names a concept that the definition does not have. Add it in Define, or remove the rule.");
        }
        return new From(typeof(T), depth, this);
    }
}
