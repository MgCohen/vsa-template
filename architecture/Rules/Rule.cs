namespace Umpire.Architecture;

public sealed class Rule : Chain<Rule> {
    internal Permission Permission;
    internal List<Entry> From = [];

    internal List<Entry> Targets => Entries;

    public Rule InSame<T>() where T : Concept => Filter(FilterKind.InSame, typeof(T));

    public Rule InOther<T>() where T : Concept => Filter(FilterKind.InOther, typeof(T));

    public override string ToString() =>
        $"{string.Join(" and ", From)} {(Permission == Permission.Allow ? "uses" : "denies")} {string.Join(" and ", Entries)}";
}
