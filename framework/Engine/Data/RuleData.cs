namespace VSA.Architecture.Data;

public sealed record RuleData(
    string Id, string Text, string Source, string Access, int Pairs, List<TermData> From, List<TermData> To);
