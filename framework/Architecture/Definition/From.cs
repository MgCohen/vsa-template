namespace VSA.Architecture;

public sealed class From : Chain<From> {
    readonly List<IRuleEntries> rules;
    readonly string source;

    internal From(Type layer, Reach? reach, string source, List<IRuleEntries> rules) {
        this.source = source;
        this.rules = rules;
        Begin(layer, reach);
    }

    public Uses Uses<T>(Reach? reach = null) where T : Layer => Uses<T>(null, reach);

    public Uses Uses<T>(Access? access, Reach? reach = null) where T : Layer => Start(new Uses(), typeof(T), reach, access, Permission.Allow);

    public Denies Denies<T>(Reach? reach = null) where T : Layer => Start(new Denies(), typeof(T), reach, null, Permission.Deny);

    TRule Start<TRule>(TRule rule, Type to, Reach? reach, Access? access, Permission permission) where TRule : Rule<TRule> {
        rule.Permission = permission;
        rule.From = Entries;
        rule.Source = source;
        rule.Begin(to, reach, access);
        rules.Add(rule);
        return rule;
    }
}
