namespace VSA.Architecture;

public abstract class Rule<TSelf> : Chain<TSelf>, IRuleEntries where TSelf : Rule<TSelf> {
    internal Permission Permission;
    internal List<Entry> From = [];
    internal string Source = "";

    List<Entry> IRuleEntries.From => From;
    List<Entry> IRuleEntries.Targets => Entries;
    Permission IRuleEntries.Permission => Permission;
    string IRuleEntries.Source => Source;
    string IRuleEntries.Text => ToString();

    public TSelf InSame<T>() where T : Concept => Filter("InSame", typeof(T));

    public TSelf InOther<T>() where T : Concept => Filter("InOther", typeof(T));

    public override string ToString() =>
        $"{string.Join(" and ", From)} {(Permission == Permission.Allow ? "uses" : "denies")} {string.Join(" and ", Entries)}";
}
