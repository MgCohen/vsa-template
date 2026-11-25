namespace Umpire.CheckModel;

public sealed record CountRule(
    IReadOnlyList<RuleEntry> Subjects,
    IReadOnlyList<int> Parts,
    bool IsWholeApp,
    bool IsPerPart,
    bool SkipsTaken,
    bool IsRequired,
    CountedTypes Counted,
    bool OneKind,
    int? Min = null,
    int? Max = null) {
    public string Counts => Counted.Concept is { } concept
        ? $"{concept.Display} types"
        : Counted.Type is { } type ? $"types of {type.Name}" : "types";

    public string Limits => Min is not null && Min == Max
        ? $"exactly {Min}"
        : Joined(Min is null ? "" : $"at least {Min}", Max is null ? "" : $"at most {Max}");

    public override string ToString() {
        var counted = Counted.Concept is { } concept ? concept.Display : Counts;
        var depth = Counted.Depth is { } own ? $"({own})" : "";
        return $"{RuleEntry.Text(Subjects)} has {counted}{depth} {Joined(Limits, OneKind ? "of one kind" : "")}";
    }

    static string Joined(params string[] texts) => string.Join(", ", texts.Where(text => text.Length > 0));
}
