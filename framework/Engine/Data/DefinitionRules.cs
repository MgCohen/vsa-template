namespace VSA.Architecture.Data;

public static class DefinitionRules {
    public static IReadOnlyList<NamedRule> Of(ArchitectureDefinition definition) =>
        [.. definition.RuleEntries().Select(r => new NamedRule(
            r.Text, r.Source, r.Permission, [.. r.From.Select(TermOf)], [.. r.Targets.Select(TermOf)]))];

    public static string Name(Type concept) => Concept.Display(concept);

    static NamedTerm TermOf(Entry entry) =>
        new(Name(entry.Target), [.. entry.Filters.Select(f => new NamedFilter(f.Kind, Name(f.Target)))]);
}
