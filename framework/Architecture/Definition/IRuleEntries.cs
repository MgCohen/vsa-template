namespace VSA.Architecture;

internal interface IRuleEntries {
    List<Entry> From { get; }
    List<Entry> Targets { get; }
    Permission Permission { get; }
    string Source { get; }
    string Text { get; }
}
