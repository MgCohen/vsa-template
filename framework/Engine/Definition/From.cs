namespace VSA.Architecture;

public sealed class From : Chain<From> {
    readonly List<IRuleEntries> rules;
    readonly string source;

    internal From(Type concept, Depth? depth, string source, List<IRuleEntries> rules) {
        this.source = source;
        this.rules = rules;
        Begin(concept, depth);
    }

    public Uses Uses<T>(Depth? depth = null) where T : Concept => Uses<T>(null, depth);

    public Uses Uses<T>(Access? access, Depth? depth = null) where T : Concept => Start(new Uses(), typeof(T), depth, access, Permission.Allow);

    public Uses Uses<T>(Boundary boundary, Access? access = null, Depth? depth = null) where T : Concept =>
        Start(new Uses(), typeof(T), depth, access, Permission.Allow, boundary);

    public Denies Denies<T>(Depth? depth = null) where T : Concept => Start(new Denies(), typeof(T), depth, null, Permission.Deny);

    TRule Start<TRule>(TRule rule, Type to, Depth? depth, Access? access, Permission permission, Boundary? boundary = null) where TRule : Rule<TRule> {
        rule.Permission = permission;
        rule.From = Entries;
        rule.Source = source;
        rule.Begin(to, depth, access, boundary);
        rules.Add(rule);
        return rule;
    }
}
