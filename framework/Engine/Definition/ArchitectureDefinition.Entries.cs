namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    internal IReadOnlyList<IRuleEntries> RuleEntries() {
        var book = new RuleBook(Root);
        foreach (var concept in Parts().Select(p => p.Concept!).GroupBy(c => c.Identity).Select(g => g.First())) {
            concept.Rules(new ConceptRules(concept.Identity, book.Rules));
        }
        Rules(book);
        return book.Rules;
    }
}
