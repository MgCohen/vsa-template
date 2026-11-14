using Viewer.Export.Model;
using VSA.Architecture;

namespace Viewer.Export;

public sealed class DefinitionExport
{
    private readonly ArchitectureDefinition definition;
    private readonly IReadOnlyList<PartRule> pairs;

    public DefinitionExport(ArchitectureDefinition definition)
    {
        this.definition = definition;
        pairs = definition.Resolve();
        PartIds = definition.Root.Walk().Select((p, i) => (p, i)).ToDictionary(x => x.p, x => $"p{x.i}");
        RuleIds = pairs.Select(p => p.Text).Distinct().Select((text, i) => (text, i))
            .ToDictionary(x => x.text, x => $"r{x.i}");
    }

    public IReadOnlyDictionary<Part, string> PartIds { get; }

    public IReadOnlyDictionary<string, string> RuleIds { get; }

    public DefinitionData Data()
    {
        Dictionary<string, IRuleEntries> entries = definition.RuleEntries()
            .GroupBy(r => r.Text).ToDictionary(g => g.Key, g => g.First());
        List<RuleData> rules = [.. pairs.GroupBy(p => p.Text).Select(g => RuleOf(g.ToList(), entries))];
        List<PairData> pairData = [.. pairs.Select(PairOf)];
        string name = definition.GetType().Name;
        string unlisted = definition.Options.DefaultPermission.ToString();
        return new DefinitionData(name, unlisted, Parts(), rules, pairData);
    }

    private List<PartData> Parts()
    {
        return [.. definition.Root.Walk().Select(p => new PartData(
            PartIds[p], p.Name, p.Parent is null ? null : PartIds[p.Parent], p.Path, p.IsGroup, p.HasFolder,
            [.. p.Types.Select(t => t.Name)]))];
    }

    private RuleData RuleOf(List<PartRule> group, IReadOnlyDictionary<string, IRuleEntries> entries)
    {
        PartRule first = group[0];
        IRuleEntries? rule = entries.GetValueOrDefault(first.Text);
        List<TermData> from = rule is null ? [] : [.. rule.From.Select(RuleTerms.Of)];
        List<TermData> to = rule is null ? [] : [.. rule.Targets.Select(RuleTerms.Of)];
        string access = first.Permission.ToString();
        return new RuleData(RuleIds[first.Text], first.Text, first.Source, access, group.Count, from, to);
    }

    private PairData PairOf(PartRule pair, int order)
    {
        string permission = pair.Permission.ToString();
        string rule = RuleIds[pair.Text];
        return new PairData(
            PartIds[pair.From], PartIds[pair.To], pair.Condition, permission, rule, order, pair.Access.ToString());
    }
}
