namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    internal IReadOnlyList<IRuleEntries> RuleEntries() {
        var map = new Map(Root);
        foreach (var layer in Parts().Select(p => p.Layer!).DistinctBy(l => l.GetType())) {
            layer.Rules(new LayerRules(layer.GetType(), map.Rules));
        }
        Rules(map);
        return map.Rules;
    }
}
