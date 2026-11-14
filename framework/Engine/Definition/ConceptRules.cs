namespace VSA.Architecture;

public sealed class ConceptRules {
    readonly From from;

    internal ConceptRules(Type concept, List<IRuleEntries> rules) {
        from = new From(concept, null, $"concept {Concept.Display(concept)}", rules);
    }

    public Uses Uses<T>(Depth? depth = null) where T : Concept => from.Uses<T>(depth);

    public Uses Uses<T>(Access? access, Depth? depth = null) where T : Concept => from.Uses<T>(access, depth);

    public Denies Denies<T>(Depth? depth = null) where T : Concept => from.Denies<T>(depth);
}
