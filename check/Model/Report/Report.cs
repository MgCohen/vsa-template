namespace Umpire.CheckModel;

public sealed class Report {
    readonly List<Finding> findings = [];
    readonly Dictionary<string, string> metadata = new(StringComparer.Ordinal);

    public bool IsComplete { get; private set; } = true;
    public bool Succeeded => Reason is null;
    public string? Reason { get; private set; }
    public IReadOnlyList<Finding> Findings => findings;
    public IReadOnlyDictionary<string, string> Metadata => metadata;
    public bool HasErrors => findings.Any(f => f.Severity == Severity.Error);

    public IReadOnlyList<string> Lines => [.. findings.Distinct()
        .OrderBy(f => f.File is not null)
        .ThenBy(f => f.File, StringComparer.Ordinal)
        .ThenBy(f => f.Line)
        .ThenBy(f => f.Column)
        .ThenBy(f => f.Code, StringComparer.Ordinal)
        .ThenBy(f => f.Text, StringComparer.Ordinal)
        .Select(f => f.ToString())];

    public void Add(Finding finding) => findings.Add(finding);

    public void Doubt() => IsComplete = false;

    public void Describe(string name, string value) => metadata[name] = value;

    public void Fail(string reason) => Reason ??= reason;
}
