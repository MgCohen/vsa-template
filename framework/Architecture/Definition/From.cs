namespace VSA.Architecture;

public sealed class From : Chain<From> {
    readonly List<IRuleEntries> rules;
    readonly string source;

    internal From(Type layer, Reach? reach, string source, List<IRuleEntries> rules) {
        this.source = source;
        this.rules = rules;
        Begin(layer, reach);
    }

    public Uses Uses<T>(Reach? reach = null) where T : Layer => Start(new Uses(), typeof(T), reach, Access.Allow);

    public Denies Denies<T>(Reach? reach = null) where T : Layer => Start(new Denies(), typeof(T), reach, Access.Deny);

    TRule Start<TRule>(TRule rule, Type to, Reach? reach, Access access) where TRule : Rule<TRule> {
        rule.Access = access;
        rule.From = Entries;
        rule.Source = source;
        rule.Begin(to, reach);
        rules.Add(rule);
        return rule;
    }
}
