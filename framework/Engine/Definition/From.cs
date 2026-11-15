namespace VSA.Architecture;

public sealed class From : Chain<From> {
    readonly List<Rule> rules;

    internal From(Type concept, Depth? depth, List<Rule> rules) {
        this.rules = rules;
        Begin(concept, depth);
    }

    public Rule Uses<T>(Depth? depth = null) where T : Concept => Start(typeof(T), Permission.Allow, depth, null, null);

    public Rule Uses<T>(Access? access) where T : Concept => Start(typeof(T), Permission.Allow, null, access, null);

    public Rule Uses<T>(Boundary boundary) where T : Concept => Start(typeof(T), Permission.Allow, null, null, boundary);

    public Rule Denies<T>(Depth? depth = null) where T : Concept => Start(typeof(T), Permission.Deny, depth, null, null);

    Rule Start(Type to, Permission permission, Depth? depth, Access? access, Boundary? boundary) {
        var rule = new Rule { Permission = permission, From = Entries };
        rule.Begin(to, depth, access, boundary);
        rules.Add(rule);
        return rule;
    }
}
