namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    internal IReadOnlyList<IRuleEntries> RuleEntries() {
        var book = new RuleBook(Root);
        foreach (var layer in Parts().Select(p => p.Layer!).GroupBy(l => l.GetType()).Select(g => g.First())) {
            layer.Rules(new LayerRules(layer.GetType(), book.Rules));
        }
        Rules(book);
        return book.Rules;
    }
}
