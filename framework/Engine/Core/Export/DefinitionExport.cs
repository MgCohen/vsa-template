using VSA.Architecture.Data;

namespace VSA.Architecture.Engine;

sealed class DefinitionExport {
    readonly ArchitectureDefinition definition;
    readonly IReadOnlyList<PartRule> rules;

    public DefinitionExport(ArchitectureDefinition definition) {
        this.definition = definition;
        rules = definition.Resolve();
        PartIds = definition.Root.Walk().Select((part, index) => new { part, index })
            .ToDictionary(p => p.part, p => $"p{p.index}");
        RuleIds = rules.Select(r => r.Text).Distinct().Select((text, index) => new { text, index })
            .ToDictionary(r => r.text, r => $"r{r.index}");
    }

    public IReadOnlyDictionary<Part, string> PartIds { get; }

    public IReadOnlyDictionary<string, string> RuleIds { get; }

    public DefinitionData Data() {
        var entries = definition.RuleEntries().GroupBy(r => r.Text).ToDictionary(g => g.Key, g => g.First());
        var options = definition.Options;
        return new DefinitionData(
            definition.GetType().Name,
            new OptionsData(options.DefaultPermission.ToString(), options.Boundary.ToString(), options.UseSameConcept,
                options.Depth.ToString(), options.Access.ToString()),
            [.. definition.Root.Walk().Select(PartOf)],
            [.. rules.GroupBy(r => r.Text).Select(g => RuleOf([.. g], entries))],
            [.. rules.Select(PairOf)]);
    }

    PartData PartOf(Part part) => new(
        PartIds[part], part.Name, part.Parent is null ? null : PartIds[part.Parent], part.Path, part.IsGroup, part.HasFolder,
        [.. part.Types.Select(t => t.Name)]);

    RuleData RuleOf(List<PartRule> group, IReadOnlyDictionary<string, IRuleEntries> entries) {
        var first = group[0];
        var entry = entries.GetValueOrDefault(first.Text);
        List<TermData> from = entry is null ? [] : [.. entry.From.Select(TermOf)];
        List<TermData> to = entry is null ? [] : [.. entry.Targets.Select(TermOf)];
        return new RuleData(RuleIds[first.Text], first.Text, first.Source, first.Permission.ToString(), group.Count, from, to);
    }

    PairData PairOf(PartRule rule, int order) => new(
        PartIds[rule.From], PartIds[rule.To], rule.Condition, rule.Permission.ToString(), RuleIds[rule.Text], order,
        rule.Access.ToString());

    static TermData TermOf(Entry entry) => new(
        Concept.Display(entry.Target), entry.Depth?.ToString(), entry.Access?.ToString(),
        [.. entry.Filters.Select(f => new FilterData(f.Kind.ToString(), Concept.Display(f.Target)))]);
}
