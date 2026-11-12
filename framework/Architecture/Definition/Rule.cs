namespace VSA.Architecture;

public abstract class Rule<TSelf> : Chain<TSelf>, IRuleEntries where TSelf : Rule<TSelf> {
    internal Access Access;
    internal List<Entry> From = [];
    internal string Source = "";

    List<Entry> IRuleEntries.From => From;
    List<Entry> IRuleEntries.Targets => Entries;
    Access IRuleEntries.Access => Access;
    string IRuleEntries.Source => Source;
    string IRuleEntries.Text => ToString();

    public TSelf InOwn<T>() where T : Layer => Filter("InOwn", typeof(T));

    public TSelf InOther<T>() where T : Layer => Filter("InOther", typeof(T));

    public override string ToString() =>
        $"{string.Join(" and ", From)} {(Access == Access.Allow ? "uses" : "denies")} {string.Join(" and ", Entries)}";
}
